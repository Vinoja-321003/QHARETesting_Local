using AutoFixture;
using Bogus;

namespace QualiFrame.Tests.Utilities
{
    /// <summary>
    /// Uses Bogus and AutoFixture so those test-project packages have a real reuse path.
    /// Keep this in QualiFrame.Tests — the packages are not Framework dependencies.
    /// </summary>
    public static class SyntheticTestDataFactory
    {
        public static (string Email, string UserName, string FirstName, string LastName) CreateUser(int seed = 42)
        {
            var faker = new Faker("en") { Random = new Randomizer(seed) };
            return (faker.Internet.Email(), faker.Internet.UserName(), faker.Name.FirstName(), faker.Name.LastName());
        }

        public static T CreateAnonymous<T>()
        {
            return new Fixture().Create<T>();
        }
    }
}
