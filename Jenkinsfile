pipeline {
    agent any

    environment {
        BUILD_CONFIGURATION = 'Release'
        TEST_PROJECT = 'src/QualiFrame.Tests/QualiFrame.Tests.csproj'
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Validate Manifest') {
            steps {
                sh 'bash scripts/ci-validate-manifest.sh'
            }
        }

        stage('Build and Test') {
            steps {
                sh 'bash scripts/ci-build-test.sh'
            }
            post {
                always {
                    junit '**/test-results.trx'
                    publishHTML([
                        reportDir: 'Results/Reports',
                        reportFiles: '*.html',
                        reportName: 'Test Report'
                    ])
                }
            }
        }

        stage('Publish Results') {
            steps {
                archiveArtifacts artifacts: 'Results/**/*', fingerprint: true
                publishTestResults testResultsPattern: '**/*.trx'
            }
        }
    }

    post {
        always {
            cleanWs()
        }
        success {
            echo 'Pipeline succeeded!'
        }
        failure {
            echo 'Pipeline failed!'
        }
    }
}
