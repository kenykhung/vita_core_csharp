#addin "nuget:?package=Cake.Coveralls&version=0.10.1"
#addin "nuget:?package=Cake.Git&version=0.21.0"
#addin "nuget:?package=Cake.ReSharperReports&version=0.11.1"
#addin "nuget:?package=Cake.Sonar&version=1.1.22"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var configuration = Argument("configuration", "Debug");
var revision = EnvironmentVariable("BUILD_NUMBER") ?? Argument("revision", "9999");
var target = Argument("target", "Default");
var unitTesting = EnvironmentVariable("UNIT_TESTING") ?? "ON";


//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define git commit id
var commitId = "SNAPSHOT";

// Define product name and version
var product = "Htc.Vita.Core";
var companyName = "HTC";
var version = "0.10.3";
var semanticVersion = string.Format("{0}.{1}", version, revision);
var ciVersion = string.Format("{0}.{1}", version, "0");

// Define copyright
var copyright = string.Format("Copyright © 2017 - {0}", DateTime.Now.Year);

// Define timestamp for signing
var lastSignTimestamp = DateTime.Now;
var signIntervalInMilli = 1000 * 5;

// Define path
var solutionFile = File(string.Format("./source/{0}.sln", product));

// Define directories.
var distDir = Directory("./dist");
var tempDir = Directory("./temp");
var generatedDir = Directory("./source/generated");
var packagesDir = Directory("./source/packages");
var nugetDir = distDir + Directory(configuration) + Directory("nuget");
var homeDir = Directory(EnvironmentVariable("USERPROFILE") ?? EnvironmentVariable("HOME"));
var reportDotCoverDirAnyCPU = distDir + Directory(configuration) + Directory("report/dotCover/AnyCPU");
var reportDotCoverDirX86 = distDir + Directory(configuration) + Directory("report/dotCover/x86");
var reportOpenCoverDirAnyCPU = distDir + Directory(configuration) + Directory("report/OpenCover/AnyCPU");
var reportOpenCoverDirX86 = distDir + Directory(configuration) + Directory("report/OpenCover/x86");
var reportXUnitDirAnyCPU = distDir + Directory(configuration) + Directory("report/xUnit/AnyCPU");
var reportXUnitDirX86 = distDir + Directory(configuration) + Directory("report/xUnit/x86");
var reportReSharperDupFinder = distDir + Directory(configuration) + Directory("report/ReSharper/DupFinder");
var reportReSharperInspectCode = distDir + Directory(configuration) + Directory("report/ReSharper/InspectCode");

// Define signing key, password and timestamp server
var signKeyEnc = EnvironmentVariable("SIGNKEYENC") ?? "NOTSET";
var signPass = EnvironmentVariable("SIGNPASS") ?? "NOTSET";
var signSha1Uri = new Uri("http://timestamp.digicert.com");
var signSha256Uri = new Uri("http://timestamp.digicert.com");

// Define coveralls update key
var coverallsApiKey = EnvironmentVariable("COVERALLS_APIKEY") ?? "NOTSET";

// Define nuget push source and key
var nugetApiKey = EnvironmentVariable("NUGET_PUSH_TOKEN") ?? EnvironmentVariable("NUGET_APIKEY") ?? "NOTSET";
var nugetSource = EnvironmentVariable("NUGET_PUSH_PATH") ?? EnvironmentVariable("NUGET_SOURCE") ?? "NOTSET";

// Define sonarcloud key
var sonarcloudApiKey = EnvironmentVariable("SONARCLOUD_APIKEY") ?? "NOTSET";


//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Fetch-Git-Commit-ID")
    .ContinueOnError()
    .Does(() =>
{
    var lastCommit = GitLogTip(MakeAbsolute(Directory(".")));
    commitId = lastCommit.Sha;
});

Task("Display-Config")
    .IsDependentOn("Fetch-Git-Commit-ID")
    .Does(() =>
{
    Information("Build target: {0}", target);
    Information("Build configuration: {0}", configuration);
    Information("Build commitId: {0}", commitId);
    if ("Release".Equals(configuration))
    {
        Information("Build version: {0}", semanticVersion);
    }
    else
    {
        Information("Build version: {0}-CI{1}", ciVersion, revision);
    }
});

Task("Clean-Workspace")
    .IsDependentOn("Display-Config")
    .Does(() =>
{
    CleanDirectory(distDir);
    CleanDirectory(tempDir);
    CleanDirectory(generatedDir);
    CleanDirectory(packagesDir);
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean-Workspace")
    .Does(() =>
{
    NuGetRestore(string.Format("./source/{0}.sln", product));
});

Task("Generate-AssemblyInfo")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    CreateDirectory(generatedDir);
    var file = "./source/generated/SharedAssemblyInfo.cs";
    var assemblyVersion = semanticVersion;
    if (!"Release".Equals(configuration))
    {
        assemblyVersion = ciVersion;
    }
    CreateAssemblyInfo(
            file,
            new AssemblyInfoSettings
            {
                    Company = companyName,
                    Copyright = copyright,
                    Product = string.Format("{0} : {1}", product, commitId),
                    Version = version,
                    FileVersion = assemblyVersion,
                    InformationalVersion = assemblyVersion
            }
    );
});

Task("Run-Sonar-Begin")
    .WithCriteria(() => !"NOTSET".Equals(sonarcloudApiKey))
    .IsDependentOn("Generate-AssemblyInfo")
    .Does(() =>
{
    SonarBegin(
            new SonarBeginSettings {
                    Url = "https://sonarcloud.io",
                    Login = sonarcloudApiKey,
                    Key = "ViveportSoftware_vita_core_csharp",
                    Organization = "viveportsoftware",
                    OpenCoverReportsPath = "**/*.OpenCover.xml"
            }
    );
});

Task("Build-Assemblies")
    .IsDependentOn("Run-Sonar-Begin")
    .Does(() =>
{
    var settings = new DotNetCoreBuildSettings
    {
            Configuration = configuration
    };
    DotNetCoreBuild("./source/", settings);
});

Task("Prepare-Unit-Test-Data")
    .WithCriteria(() => "ON".Equals(unitTesting))
    .IsDependentOn("Build-Assemblies")
    .Does(() =>
{
    if (!FileExists(homeDir + File("TestData.Md5.txt")))
    {
        CopyFileToDirectory("source/" + product + ".Tests/TestData.Md5.txt", homeDir);
    }
    if (!FileExists(homeDir + File("TestData.Sha1.txt")))
    {
        CopyFileToDirectory("source/" + product + ".Tests/TestData.Sha1.txt", homeDir);
    }
});

Task("Run-Unit-Tests-Under-AnyCPU")
    .WithCriteria(() => "ON".Equals(unitTesting))
    .IsDependentOn("Prepare-Unit-Test-Data")
    .Does(() =>
{
    CreateDirectory(reportXUnitDirAnyCPU);
    if(IsRunningOnWindows())
    {
        DotCoverAnalyse(
                tool =>
                {
                        tool.XUnit2(
                                "./temp/" + configuration + "/" + product + ".Tests/bin/AnyCPU/net452/*.Tests.dll",
                                new XUnit2Settings
                                {
                                        Parallelism = ParallelismOption.None,
                                        HtmlReport = true,
                                        NUnitReport = true,
                                        OutputDirectory = reportXUnitDirAnyCPU
                                }
                        );
                },
                new FilePath(reportDotCoverDirAnyCPU.ToString() + "/" + product + ".html"),
                new DotCoverAnalyseSettings
                {
                        ReportType = DotCoverReportType.HTML
                }.WithFilter("+:*")
                .WithFilter("-:xunit.*")
                .WithFilter("-:*.NunitTest")
                .WithFilter("-:*.Tests")
                .WithFilter("-:*.XunitTest")
        );
        CreateDirectory(reportOpenCoverDirAnyCPU);
        var openCoverSettings = new OpenCoverSettings
        {
                MergeByHash = true,
                NoDefaultFilters = true,
                Register = "user",
                SkipAutoProps = true
        }.WithFilter("+[*]*")
        .WithFilter("-[xunit.*]*")
        .WithFilter("-[*.NunitTest]*")
        .WithFilter("-[*.Tests]*")
        .WithFilter("-[*.XunitTest]*");
        OpenCover(
                tool =>
                {
                        tool.XUnit2(
                                "./temp/" + configuration + "/" + product + ".Tests/bin/AnyCPU/net452/*.Tests.dll",
                                new XUnit2Settings
                                {
                                        Parallelism = ParallelismOption.None,
                                        OutputDirectory = reportXUnitDirAnyCPU,
                                        ShadowCopy = false
                                }
                        );
                },
                new FilePath(reportOpenCoverDirAnyCPU.ToString() + "/" + product + ".OpenCover.xml"),
                openCoverSettings
        );
    }
    else
    {
        XUnit2(
                "./temp/" + configuration + "/" + product + ".Tests/bin/AnyCPU/net452/*.Tests.dll",
                new XUnit2Settings
                {
                        Parallelism = ParallelismOption.None,
                        HtmlReport = true,
                        NUnitReport = true,
                        OutputDirectory = reportXUnitDirAnyCPU
                }
        );
    }
});

Task("Run-Unit-Tests-Under-X86")
    .WithCriteria(() => "ON".Equals(unitTesting))
    .IsDependentOn("Run-Unit-Tests-Under-AnyCPU")
    .Does(() =>
{
    CreateDirectory(reportXUnitDirX86);
    if(IsRunningOnWindows())
    {
        DotCoverAnalyse(
                tool =>
                {
                        tool.XUnit2(
                                "./temp/" + configuration + "/" + product + ".Tests/bin/x86/net452/*.Tests.dll",
                                new XUnit2Settings
                                {
                                        Parallelism = ParallelismOption.None,
                                        HtmlReport = true,
                                        NUnitReport = true,
                                        UseX86 = true,
                                        OutputDirectory = reportXUnitDirX86
                                }
                        );
                },
                new FilePath(reportDotCoverDirX86.ToString() + "/" + product + ".html"),
                new DotCoverAnalyseSettings
                {
                        ReportType = DotCoverReportType.HTML
                }.WithFilter("+:*")
                .WithFilter("-:xunit.*")
                .WithFilter("-:*.NunitTest")
                .WithFilter("-:*.Tests")
                .WithFilter("-:*.XunitTest")
        );
    }
    else
    {
        XUnit2(
                "./temp/" + configuration + "/" + product + ".Tests/bin/x86/net452/*.Tests.dll",
                new XUnit2Settings
                {
                        Parallelism = ParallelismOption.None,
                        HtmlReport = true,
                        NUnitReport = true,
                        UseX86 = true,
                        OutputDirectory = reportXUnitDirX86
                }
        );
    }
});

Task("Run-Sonar-End")
    .WithCriteria(() => !"NOTSET".Equals(sonarcloudApiKey))
    .IsDependentOn("Run-Unit-Tests-Under-X86")
    .Does(() =>
{
    SonarEnd(
            new SonarEndSettings
            {
                    Login = sonarcloudApiKey,
            }
    );
});

Task("Run-DupFinder")
    .IsDependentOn("Run-Sonar-End")
    .Does(() =>
{
    if(IsRunningOnWindows())
    {
        DupFinder(
                string.Format("./source/{0}.sln", product),
                new DupFinderSettings()
                {
                        ShowStats = true,
                        ShowText = true,
                        OutputFile = new FilePath(reportReSharperDupFinder.ToString() + "/" + product + ".xml"),
                        ThrowExceptionOnFindingDuplicates = false
                }
        );
        ReSharperReports(
                new FilePath(reportReSharperDupFinder.ToString() + "/" + product + ".xml"),
                new FilePath(reportReSharperDupFinder.ToString() + "/" + product + ".html")
        );
    }
});

Task("Run-InspectCode")
    .IsDependentOn("Run-DupFinder")
    .Does(() =>
{
    if(IsRunningOnWindows())
    {
        InspectCode(
                string.Format("./source/{0}.sln", product),
                new InspectCodeSettings()
                {
                        SolutionWideAnalysis = true,
                        OutputFile = new FilePath(reportReSharperInspectCode.ToString() + "/" + product + ".xml"),
                        ThrowExceptionOnFindingViolations = false
                }
        );
        ReSharperReports(
                new FilePath(reportReSharperInspectCode.ToString() + "/" + product + ".xml"),
                new FilePath(reportReSharperInspectCode.ToString() + "/" + product + ".html")
        );
    }
});

Task("Sign-Assemblies")
    .WithCriteria(() => "Release".Equals(configuration) && !"NOTSET".Equals(signPass) && !"NOTSET".Equals(signKeyEnc))
    .IsDependentOn("Run-InspectCode")
    .Does(() =>
{
    var currentSignTimestamp = DateTime.Now;
    Information("Last timestamp:    " + lastSignTimestamp);
    Information("Current timestamp: " + currentSignTimestamp);
    var totalTimeInMilli = (DateTime.Now - lastSignTimestamp).TotalMilliseconds;

    var signKey = "./temp/key.pfx";
    System.IO.File.WriteAllBytes(signKey, Convert.FromBase64String(signKeyEnc));

    var file = string.Format("./temp/{0}/{1}/bin/net45/{1}.dll", configuration, product);

    if (totalTimeInMilli < signIntervalInMilli)
    {
        System.Threading.Thread.Sleep(signIntervalInMilli - (int)totalTimeInMilli);
    }
    Sign(
            file,
            new SignToolSignSettings
            {
                    TimeStampUri = signSha1Uri,
                    CertPath = signKey,
                    Password = signPass
            }
    );
    lastSignTimestamp = DateTime.Now;

    System.Threading.Thread.Sleep(signIntervalInMilli);
    Sign(
            file,
            new SignToolSignSettings
            {
                    AppendSignature = true,
                    TimeStampUri = signSha256Uri,
                    DigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                    TimeStampDigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                    CertPath = signKey,
                    Password = signPass
            }
    );
    lastSignTimestamp = DateTime.Now;

    file = string.Format("./temp/{0}/{1}/bin/netcoreapp2.1/{1}.dll", configuration, product);

    if (totalTimeInMilli < signIntervalInMilli)
    {
        System.Threading.Thread.Sleep(signIntervalInMilli - (int)totalTimeInMilli);
    }
    Sign(
            file,
            new SignToolSignSettings
            {
                    TimeStampUri = signSha1Uri,
                    CertPath = signKey,
                    Password = signPass
            }
    );
    lastSignTimestamp = DateTime.Now;

    System.Threading.Thread.Sleep(signIntervalInMilli);
    Sign(
            file,
            new SignToolSignSettings
            {
                    AppendSignature = true,
                    TimeStampUri = signSha256Uri,
                    DigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                    TimeStampDigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                    CertPath = signKey,
                    Password = signPass
            }
    );
    lastSignTimestamp = DateTime.Now;

    file = string.Format("./temp/{0}/{1}/bin/netstandard2.0/{1}.dll", configuration, product);

    if (totalTimeInMilli < signIntervalInMilli)
    {
        System.Threading.Thread.Sleep(signIntervalInMilli - (int)totalTimeInMilli);
    }
    Sign(
            file,
            new SignToolSignSettings
            {
                    TimeStampUri = signSha1Uri,
                    CertPath = signKey,
                    Password = signPass
            }
    );
    lastSignTimestamp = DateTime.Now;

    System.Threading.Thread.Sleep(signIntervalInMilli);
    Sign(
            file,
            new SignToolSignSettings
            {
                    AppendSignature = true,
                    TimeStampUri = signSha256Uri,
                    DigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                    TimeStampDigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                    CertPath = signKey,
                    Password = signPass
            }
    );
    lastSignTimestamp = DateTime.Now;
});

Task("Build-NuGet-Package")
    .IsDependentOn("Sign-Assemblies")
    .Does(() =>
{
    CreateDirectory(nugetDir);
    var nugetPackVersion = semanticVersion;
    if (!"Release".Equals(configuration))
    {
        nugetPackVersion = string.Format("{0}-CI{1}", ciVersion, revision);
    }
    Information("Pack version: {0}", nugetPackVersion);
    var settings = new DotNetCorePackSettings
    {
            Configuration = configuration,
            OutputDirectory = nugetDir,
            NoBuild = true,
            ArgumentCustomization = (args) =>
            {
                    return args.Append("/p:Version={0}", nugetPackVersion);
            }
    };

    DotNetCorePack("./source/" + product + "/", settings);
});

Task("Update-Coverage-Report")
    .WithCriteria(() => !"NOTSET".Equals(coverallsApiKey))
    .IsDependentOn("Build-NuGet-Package")
    .Does(() =>
{
    CoverallsIo(
            reportOpenCoverDirAnyCPU.ToString() + "/" + product + ".OpenCover.xml",
            new CoverallsIoSettings()
            {
                    RepoToken = coverallsApiKey
            }
    );
});

Task("Publish-NuGet-Package")
    .WithCriteria(() => "Release".Equals(configuration) && !"NOTSET".Equals(nugetApiKey) && !"NOTSET".Equals(nugetSource))
    .IsDependentOn("Update-Coverage-Report")
    .Does(() =>
{
    var nugetPushVersion = semanticVersion;
    if (!"Release".Equals(configuration))
    {
        nugetPushVersion = string.Format("{0}-CI{1}", ciVersion, revision);
    }
    Information("Publish version: {0}", nugetPushVersion);
    var package = string.Format("./dist/{0}/nuget/{1}.{2}.nupkg", configuration, product, nugetPushVersion);
    NuGetPush(
            package,
            new NuGetPushSettings
            {
                    Source = nugetSource,
                    ApiKey = nugetApiKey
            }
    );
});


//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Update-Coverage-Report");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
