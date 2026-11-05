#!/usr/bin/env python3
"""
Scan all .csproj PackageReferences in the repo and report latest stable versions from NuGet.org.

Why this exists:
- `dotnet list package --outdated` can be slow/hang depending on restore state.
- This script directly queries NuGet v3 flatcontainer metadata for each package id.

Notes:
- Reports only *direct* PackageReference items that include an explicit Version attribute.
- Latest "stable" means: highest SemVer without a prerelease label (no '-').
"""

from __future__ import annotations

import json
import os
import re
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, Iterable, List, Optional, Set, Tuple


@dataclass(frozen=True)
class PackageRef:
    project: str
    package_id: str
    current_version: str


def _strip_ns(tag: str) -> str:
    if "}" in tag:
        return tag.split("}", 1)[1]
    return tag


def parse_csproj_packages(csproj_path: Path) -> List[Tuple[str, str]]:
    """
    Returns list of (package_id, version) for explicit PackageReference Include + Version.
    """
    try:
        xml = csproj_path.read_text(encoding="utf-8", errors="replace")
    except Exception:
        return []

    try:
        root = ET.fromstring(xml)
    except ET.ParseError:
        return []

    pkgs: List[Tuple[str, str]] = []
    for elem in root.iter():
        if _strip_ns(elem.tag) != "PackageReference":
            continue
        include = elem.attrib.get("Include") or elem.attrib.get("Update")
        if not include:
            continue
        version = elem.attrib.get("Version")
        if not version:
            # Support <PackageReference Include="X"><Version>1.2.3</Version></PackageReference>
            for child in elem:
                if _strip_ns(child.tag) == "Version" and (child.text or "").strip():
                    version = (child.text or "").strip()
                    break
        if version:
            pkgs.append((include.strip(), version.strip()))
    return pkgs


_SEMVER_RE = re.compile(
    r"^(?P<maj>0|[1-9]\d*)\.(?P<min>0|[1-9]\d*)\.(?P<pat>0|[1-9]\d*)"
    r"(?:\.(?P<rev>0|[1-9]\d*))?"
    r"(?P<pre>-[0-9A-Za-z\.-]+)?"
    r"(?P<meta>\+[0-9A-Za-z\.-]+)?$"
)


def semver_key(v: str) -> Tuple[int, int, int, int, int, str]:
    """
    Sort key for NuGet versions:
    - Prefer stable over prerelease.
    - Then compare numeric segments.
    - Prerelease label used only for tie-break (stable gets empty string and higher flag).
    """
    m = _SEMVER_RE.match(v)
    if not m:
        # Fallback: keep unknown versions last
        return (0, 0, 0, 0, 0, v)
    maj = int(m.group("maj"))
    minn = int(m.group("min"))
    pat = int(m.group("pat"))
    rev = int(m.group("rev") or 0)
    pre = m.group("pre") or ""
    is_stable = 1 if pre == "" else 0
    # stable should sort after prerelease for same numbers -> use is_stable as last numeric
    return (maj, minn, pat, rev, is_stable, pre)


def is_stable(v: str) -> bool:
    # NuGet prerelease versions contain '-'
    return "-" not in v


def fetch_versions_from_nuget(package_id: str, timeout_s: float = 15.0) -> Optional[List[str]]:
    """
    Calls NuGet flat container index for versions:
    https://api.nuget.org/v3-flatcontainer/{id}/index.json
    """
    url = f"https://api.nuget.org/v3-flatcontainer/{package_id.lower()}/index.json"
    req = urllib.request.Request(url, headers={"User-Agent": "QualiFrame-NuGet-Upgrade-Scan/1.0"})
    try:
        with urllib.request.urlopen(req, timeout=timeout_s) as resp:
            payload = resp.read().decode("utf-8", errors="replace")
        data = json.loads(payload)
        versions = data.get("versions")
        if not isinstance(versions, list):
            return None
        return [str(v) for v in versions]
    except (urllib.error.URLError, TimeoutError, json.JSONDecodeError):
        return None


def latest_stable_version(versions: Iterable[str]) -> Optional[str]:
    stables = [v for v in versions if is_stable(v)]
    if not stables:
        return None
    return sorted(stables, key=semver_key)[-1]


def main() -> int:
    repo_root = Path(__file__).resolve().parents[1]
    csprojs = sorted(repo_root.glob("**/*.csproj"))
    if not csprojs:
        print("No .csproj files found.")
        return 2

    refs: List[PackageRef] = []
    for p in csprojs:
        for pkg_id, ver in parse_csproj_packages(p):
            refs.append(PackageRef(project=str(p.relative_to(repo_root)), package_id=pkg_id, current_version=ver))

    # De-dup package ids for querying
    pkg_ids: List[str] = sorted({r.package_id for r in refs}, key=lambda s: s.lower())

    # Query NuGet with small caching
    latest_by_id: Dict[str, Optional[str]] = {}
    failed: List[str] = []

    for i, pkg_id in enumerate(pkg_ids, start=1):
        versions = fetch_versions_from_nuget(pkg_id)
        if not versions:
            latest_by_id[pkg_id] = None
            failed.append(pkg_id)
            continue
        latest_by_id[pkg_id] = latest_stable_version(versions)
        # Gentle pacing (avoid being rude to nuget.org)
        time.sleep(0.05)

    # Build report rows
    rows: List[Tuple[str, str, str, str]] = []
    for r in refs:
        latest = latest_by_id.get(r.package_id)
        latest_str = latest or "?"
        status = "up-to-date"
        if latest and semver_key(latest) > semver_key(r.current_version):
            status = "upgrade"
        elif latest is None:
            status = "unknown"
        rows.append((r.package_id, r.current_version, latest_str, r.project))

    # Print summary by package (max current across projects)
    print(f"Repo: {repo_root}")
    print(f"Projects scanned: {len(csprojs)}")
    print(f"Direct PackageReferences found: {len(refs)}")
    print(f"Unique packages: {len(pkg_ids)}")
    if failed:
        print(f"NuGet lookup failed for: {len(failed)} package(s)")
    print()

    # Aggregate by package id for clearer output
    agg: Dict[str, Dict[str, Set[str]]] = {}
    for pkg_id, cur, latest, proj in rows:
        d = agg.setdefault(pkg_id, {"current": set(), "latest": set(), "projects": set()})
        d["current"].add(cur)
        d["latest"].add(latest)
        d["projects"].add(proj)

    def best_current(versions: Set[str]) -> str:
        return sorted(versions, key=semver_key)[-1]

    report: List[Tuple[str, str, str, str]] = []
    for pkg_id, d in agg.items():
        current_best = best_current(d["current"])
        latest_best = best_current(d["latest"])
        recommendation = "up-to-date"
        if latest_best != "?" and semver_key(latest_best) > semver_key(current_best):
            recommendation = "upgrade"
        elif latest_best == "?":
            recommendation = "unknown"
        report.append((pkg_id, current_best, latest_best, recommendation))

    report.sort(key=lambda t: (t[3] != "upgrade", t[0].lower()))

    # Print as TSV for easy paste into Excel
    print("PackageId\tCurrent\tLatestStable\tRecommendation")
    for pkg_id, cur, latest, rec in report:
        print(f"{pkg_id}\t{cur}\t{latest}\t{rec}")

    # Exit non-zero if any lookups failed (useful in CI)
    return 0 if not failed else 3


if __name__ == "__main__":
    raise SystemExit(main())

