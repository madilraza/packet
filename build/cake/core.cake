#addin nuget:?package=Cake.Docker&version=0.11.0

var target = Argument("target", "Publish");
var sampleName = Argument("sample-name", "device-linux");

var defaultDockerRegistry = "localhost:5000/";
var dockerRegistry = EnvironmentVariable("DOCKER_REGISTRY", defaultDockerRegistry);
var defaultConsulHttpAddr = "consul:8500";
var consulHttpAddr = EnvironmentVariable("CONSUL_HTTP_ADDR", defaultConsulHttpAddr);

var sourceVersion = Argument("source-version", string.Empty);
var buildVersion = Argument("build-version", string.Empty);
var projectVersion = Argument("project-version", string.Empty);
var packageVersion = Argument("package-version", string.Empty);

var sourceRegistry = Argument("source-registry", string.Empty);
if (string.IsNullOrEmpty(sourceRegistry)) {
  sourceRegistry = dockerRegistry;
}
var packageRegistry = Argument("package-registry", string.Empty);
if (string.IsNullOrEmpty(packageRegistry)) {
  packageRegistry = dockerRegistry;
}

private string GetSampleImageReference() => $"{EnvironmentVariable("SAMPLE_REGISTRY")}sample-{EnvironmentVariable("SAMPLE_NAME")}:{EnvironmentVariable("SAMPLE_TAG")}";

Task("Init")
  .Does(() => {
    StartProcess("docker", "version");
    StartProcess("docker-compose", "version");

    var settings = new DockerComposeBuildSettings {
    };
    var services = new [] { "gitversion" };
    DockerComposeBuild(settings, services);
  });

Task("Version")
  .IsDependentOn("Init")
  .Does((context) => {
    if (string.IsNullOrEmpty(sourceVersion)) {
      {
        var settings = new DockerComposeUpSettings {
        };
        var services = new [] { "gitversion" };
        DockerComposeUp(settings, services);
      }

      {
        var runner = new GenericDockerComposeRunner<DockerComposeLogsSettings>(
          context.FileSystem,
          context.Environment,
          context.ProcessRunner,
          context.Tools
        );
        var settings = new DockerComposeLogsSettings {
          NoColor = true
        };
        var service = "gitversion";
        var output = runner.RunWithResult(
          "logs",
          settings,
          (items) => items.Where(item => item.Contains('|')).ToArray(),
          service
        ).Last();

        sourceVersion = output.Split('|')[1].Trim();
      }
    }
    Information($"Source version: '{sourceVersion}'.");

    if (string.IsNullOrEmpty(buildVersion)) {
      buildVersion = $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    }
    Information($"Build version: '{buildVersion}'.");

    if (string.IsNullOrEmpty(projectVersion)) {
      projectVersion = sourceVersion;
    }
    Information($"Project version: '{projectVersion}'.");

    if (string.IsNullOrEmpty(packageVersion)) {
      packageVersion = sourceVersion;
    }
    Information($"Package version: '{packageVersion}'.");

    Environment.SetEnvironmentVariable("SAMPLE_REGISTRY", sourceRegistry);
    Environment.SetEnvironmentVariable("SAMPLE_NAME", sampleName);
    Environment.SetEnvironmentVariable("SAMPLE_TAG", sourceVersion);
  });

Task("RestoreCore")
  .IsDependentOn("Version")
  .Does(() => {
    {
      var settings = new DockerComposeBuildSettings {
      };
      var services = new [] { "registry", "consul" };
      DockerComposeBuild(settings, services);
    }

    if (dockerRegistry == defaultDockerRegistry) {
      var settings = new DockerComposeUpSettings {
        DetachedMode = true
      };
      var services = new [] { "registry" };
      DockerComposeUp(settings, services);
    }

    if (consulHttpAddr == defaultConsulHttpAddr) {
      var settings = new DockerComposeUpSettings {
        DetachedMode = true
      };
      var services = new [] { "consul" };
      DockerComposeUp(settings, services);
    }
  });

Task("Clean")
  .IsDependentOn("Version")
  .Does(() => {
    var settings = new DockerComposeDownSettings {
      Rmi = "all"
    };
    DockerComposeDown(settings);
  });
