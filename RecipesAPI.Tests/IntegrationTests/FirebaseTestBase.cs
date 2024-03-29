using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;

namespace RecipesAPI.Tests.IntegrationTests;

[SetUpFixture]
public class FirebaseTestHelper
{
  public const string projectId = "demo-recipesapi";
  private static readonly HttpClient httpClient = new();
  private static IContainer? firebaseContainer;
  private static bool containerReady = false;
  [OneTimeSetUp]
  public async Task StartEmulator()
  {
    firebaseContainer = new ContainerBuilder()
     // remember to build Dockerfile.firebase using docker compose
     .WithImage("bjarkt/firebase_emulator")
     .WithPortBinding(4000, true) // ui
     .WithPortBinding(8080, true) // firestore
     .WithPortBinding(9099, true) // auth
     .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(4000)))
     .Build();

    await firebaseContainer.StartAsync()
       .ConfigureAwait(false);
    containerReady = true;

    Environment.SetEnvironmentVariable("GCLOUD_PROJECT", projectId);
    Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", FirestoreHost);
    Environment.SetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST", AuthHost);

    FirebaseApp.Create();

    var requestUri = new UriBuilder(Uri.UriSchemeHttp, firebaseContainer.Hostname, firebaseContainer.GetMappedPublicPort(4000), "/").Uri;
    var resp = await httpClient.GetStringAsync(requestUri)
      .ConfigureAwait(false);
  }

  [OneTimeTearDown]
  public async Task StopEmulator()
  {
    if (firebaseContainer == null) return;
    await firebaseContainer.StopAsync();
    await firebaseContainer.DisposeAsync();
  }

  public static FirestoreDb GetDb()
  {
    if (!containerReady) throw new Exception("container not ready");
    var db = new FirestoreDbBuilder
    {
      ProjectId = projectId,
      EmulatorDetection = Google.Api.Gax.EmulatorDetection.EmulatorOnly,
    }.Build();
    return db;
  }

  public static FirebaseAuth GetAuth()
  {
    if (!containerReady) throw new Exception("container not ready");
    return FirebaseAuth.DefaultInstance;
  }

  public static string FirestoreHost => firebaseContainer != null ? $"{firebaseContainer.Hostname}:{firebaseContainer.GetMappedPublicPort(8080)}" : throw new Exception("firebaseContainer was null");
  public static string AuthHost => firebaseContainer != null ? $"{firebaseContainer.Hostname}:{firebaseContainer.GetMappedPublicPort(9099)}" : throw new Exception("firebaseContainer was null");

  public static async Task ClearFirestore()
  {
    if (!containerReady) throw new Exception("container not ready");
    var url = $"http://{FirestoreHost}/emulator/v1/projects/{projectId}/databases/(default)/documents";
    await httpClient.DeleteAsync(url);
  }

}