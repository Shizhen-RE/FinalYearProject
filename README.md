# FYDP - NeAR Me

## Environment Setup

### Frontend
- Unity Editor 2021.3

### Backend

**Server:**
- openjdk 18.0.2
- Maven 3.8

**Recommender:**
- python 3 (3.8.9)

### Deployment
- Docker
- kubectl
- gcloud

## Cloud Resources
### Firebase
For user authentication and monitoring:
https://console.firebase.google.com/project/nearmefirebase/authentication/users

If you can't open the above link, ask Alice for access.

### MongoDB
We use MongoDB Atlas deployed to Google Cloud as our backend database.

Connection string:
```
mongodb+srv://aidanfo:50VsPsP5MSUJG8AG@near-me.c9eaw.mongodb.net/?retryWrites=true&w=majority&authSource=admin
```

Use mongo shell or MongoDB Compass to access the db content with the connection string above.

### Google Cloud
For cloud deployment of our backend services (server + recommender).

- GCP project ID: nearme-356517
- GCP artifact repository: nearme-docker-repo

The service account key should be in the repo: nearme-356517-1a3b10b60f62.json

Console for kubernetes deployment monitoring:
https://console.cloud.google.com/kubernetes/list/overview?project=nearme-356517

If you can't open the above link, ask Alice for access.

## How to deploy backend to Google Cloud

1. Install `kubectl` and `gcloud` in your command line, and make sure you can build the Java project.
2. Run the following to build and deploy the Java server and the recommender app:
   ```
   chmod u+x deployStack.sh
   ./deployStack.sh
   ```
   It can take a while for the images to build.
3. As of the current configuration, only the **server pod** is exposed with external IP. You can check the external IP by:
   ```
   kubectl get nodes --output wide
   ```
   and then reach the server at:
   ```
   http://<EXTERNAL_IP>:30876/
   ```
4. When you're done, delete the stack with:
   ```
   chmod u+x deleteStack.sh
   ./deleteStack.sh
   ```
Note: You can build and run server and recommender individually by running the `deploy.sh` in the corresponding folder. Find the sub-READMEs for more detailed instructions.

Note: If you need to bash into the pods in cloud, you can:
```
kubectl get pods
kubectl exec -it <POD_NAME> -- /bin/bash
```

## How to test backend locally
The backend consists of the Java Spring-boot server and the Python Flask recommender. You can debug the server without bringing up the recommender if you don't need the recommender-related features.

Don't run unit test yet. Don't run unit test yet. Don't run unit test yet.

1. Bring up the recommender and the server at once:
   ```
   chmod u+x ./scripts/setupLocalTest.sh
   ./scripts/setupLocalTest.sh
   ```

2. The server should be available on **localhost:8080** and the recommender on **localhost:9000**. You can start the Unity project in simulation mode to test the connection, or just test via curl. There should be one **log.txt** in each the recommender folder and the server folder for monitoring purpose.

3. Cleanup after you are done testing:
   ```
   chmod u+x ./scripts/cleanupLocalTest.sh
   ./scripts/cleanupLocalTest.sh
   ```
   This will kill both the recommender and the server at once.

## How to simulate the Unity project

Note: if you are testing with backend server, make sure to change the `BASE_URL` in _ar-unity-project/Assets/Scripts/Controller/APIManager.cs_ to the address the server is listening on.

1. Open **ar-unity-project** in Unity Editor 2021.3.
2. If not already on the **LoginScene**, go to the LoginScene.
![use-login-scene-for-simulation](./resources/pictures/login-scene.png)
3. In the **Simulator** tab, choose an appropriate device model.
![simulator-device-model](./resources/pictures/simulator-device-model.png)
4. Click the play button to start simualtion.

Note: If you have never compiled the code on your local, you might see some errors when you first open the project, but that should go away after you start simulation.

## How to build the Unity project

Setup your Android phone:

1. Go to **Settings -> About phone** and tap **Build number** 7 times;
2. Go to **Settings -> System -> Developer Options** and enable
**USB debugging**;
3. Plug phone into computer and ensure it is unlocked.

Build the app via the GUI:

1. Open *ar-unity-project* in Unity;
2. Go to **File -> Build Settings... -> Platform -> Android** and press
**Switch Platform**;
3. Go to **File -> Build and Run**.

Or via the command line:

```
unity -projectPath ar-unity-project \
      -executeMethod BuildPlayer.Do \
      -logFile - \
      -batchmode \
      -quit \
      -autoRunPlayer
```

Note that `unity` is an alias for the path to the `Unity` executable.

If the build fails, then try without the `-batchmode` flag.

The player will be installed and run on your Android device when the
`-autoRunPlayer` flag is present. Omit this flag if you just want to build the
*.apk*. For other command line options, see
[this](https://docs.unity3d.com/Manual/EditorCommandLineArguments.html).

## How to run the Java project
Note: if you want to test recommender-related feature, you must build the **recommender app** before starting the server (use _recommender/README.md_ and _scripts/setupLocalTest.sh_ as references).

1. Ensure you have Maven installed and build the Spring-boot application as follows:
   ```
   cd NearMe/nearme-backend/
   mvn clean install -DskipTests
   ```

2. Run the server:
   ```
   mvn spring-boot:run
   ```
   or start the application in debug.

3. If no error, then the server should be listening on http://localhost:8080

4. Test the server by running:
   ```
   curl http://localhost:8080/healthCheck
   // should see an OK response
   ```
   or 
   ```
   ./scripts/apireq.sh [commands]
   ```
5. Ctrl + c to terminate the server.

