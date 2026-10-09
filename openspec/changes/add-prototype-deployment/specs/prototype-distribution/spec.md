## ADDED Requirements

### Requirement: Standalone Windows package
The distribution process SHALL produce a Windows x64 Release ZIP containing the executable, .NET runtime, native dependencies and all required game content.

#### Scenario: Run without development tools
- **WHEN** a tester extracts the ZIP on a supported Windows x64 computer without the .NET SDK or a separately installed .NET runtime and starts the executable
- **THEN** the game opens its normal title screen and can enter gameplay with required visuals and audio available

#### Scenario: Missing package content
- **WHEN** publication fails or required executable, runtime or content files are absent
- **THEN** packaging fails and no successful release candidate is reported

### Requirement: Explicit versioned build
The release workflow SHALL run on manual dispatch, validate its version input, run tests and package the selected source revision. It SHALL provide a ZIP, SHA-256 checksum and metadata identifying version, source commit and build time.

#### Scenario: Successful candidate
- **WHEN** an operator requests a valid version and tests and packaging succeed
- **THEN** the candidate and its checksum are downloadable as workflow artifacts with the source commit recorded

#### Scenario: Invalid candidate
- **WHEN** the version is invalid or tests or packaging fail
- **THEN** the workflow fails without making that candidate available for deployment

### Requirement: Separate controlled distribution
Uploading to itch.io SHALL require an explicit operator action selecting an existing candidate and confirming its smoke test. The upload SHALL verify the checksum, reuse the candidate without rebuilding and target the configured restricted project and Windows prototype channel. Credentials SHALL be supplied through secrets and SHALL NOT be included in packages or logs.

#### Scenario: Build without hosting credentials
- **WHEN** a candidate is built without itch.io credentials configured
- **THEN** packaging remains available and no external upload occurs

#### Scenario: Upload approved package
- **WHEN** the operator selects a tested candidate, confirms the restricted project configuration and provides configured upload credentials
- **THEN** the verified candidate is uploaded with its version preserved and without a new build

#### Scenario: Corrupt candidate
- **WHEN** the selected candidate does not match its stored checksum
- **THEN** the upload fails before transferring the build to itch.io

### Requirement: Tester handoff and verification
Each candidate SHALL include instructions covering startup, controls, known issues and feedback with the build version. Before first tester distribution, the release checklist SHALL record a smoke test of the extracted package on a second Windows computer without development prerequisites.

#### Scenario: First release acceptance
- **WHEN** an operator prepares the first tester release
- **THEN** the checklist records version, commit, test environment and results for normal startup, arena gameplay, visuals, audio and settings, and unresolved failures prevent tester release

#### Scenario: Recover a previous build
- **WHEN** a published candidate proves unusable
- **THEN** the deployment instructions describe restoring a retained previous package or withdrawing the initial download

### Requirement: Candidate identity and upload provenance
The upload workflow SHALL accept only successful, completed workflow_dispatch runs of `.github/workflows/prototype-build.yml` on main from the same repository. It SHALL require the operator-confirmed ZIP SHA-256 and compare it with both the downloaded checksum and actual ZIP bytes. Package metadata SHALL match the selected run source commit. Local release packaging SHALL reject an unclean source working tree and SHALL NOT overwrite an existing version directory.

#### Scenario: Wrong build source
- **WHEN** a selected run comes from another branch, repository or workflow, or did not complete successfully
- **THEN** the upload is rejected before butler is invoked

#### Scenario: Wrong tested package
- **WHEN** the operator-confirmed SHA-256 differs from the downloaded ZIP hash, or package metadata differs from the run commit
- **THEN** the upload is rejected even if other inputs are valid

#### Scenario: Ambiguous local build
- **WHEN** release packaging finds uncommitted source changes or an existing output directory for the requested version
- **THEN** packaging fails without overwriting that output
