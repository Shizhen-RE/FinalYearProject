using System;
using System.IO;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Storage;
using Firebase.Extensions;
using TMPro;

public class FirebaseManager : MonoBehaviour
{

    public static FirebaseManager instance;

    // collection names
    public static String PROFILE_PICS_COLLECTION = "profile_pics";

    //Firebase variables
    [Header("Firebase Auth")]
    public DependencyStatus dependencyStatus;
    public FirebaseApp app;
    public FirebaseAuth auth;
    public FirebaseUser user;
    public string userToken; // Firebase user auth token
    public Uri downloadUrl; // Firebase storage download URL
    private bool fetchingToken = false;

    [Header("Firebase Storage")]
    public FirebaseStorage storage;
    public StorageReference storageRef;

    void Awake()
    {
        Debug.Log("FirebaseManager awake");

        if (instance == null){
            instance = this;
            DontDestroyOnLoad(gameObject);
        }else if (instance != null){
            Debug.Log("FirebaseManager: instance already exists, destroying object");
            Destroy(this);
        }

        // script initializaion
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available) {
                // Create and hold a reference to your FirebaseApp,
                // where app is a Firebase.FirebaseApp property of your application class.
                Debug.Log("FirebaseManager: Setting up Firebase");
                InitializeFirebase();
            } else {
                UnityEngine.Debug.LogError(System.String.Format(
                "FirebaseManager: Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                // Firebase Unity SDK is not safe to use here.
            }
        });

    }

    void InitializeFirebase() {
        // initialize Storage
        storage = Firebase.Storage.FirebaseStorage.DefaultInstance;
        storageRef = storage.GetReferenceFromUrl("gs://nearmefirebase.appspot.com/");

        // initialize Auth
        auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
        auth.StateChanged += AuthStateChanged;
        auth.IdTokenChanged += IdTokenChanged;
    }

    ////////////////////////////////////////////////////////////////////////
    ///////////////////////////////// Auth /////////////////////////////////
    ////////////////////////////////////////////////////////////////////////

    void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        /* Assumes one FirebaseAuth and one FirebaseUser */
        bool signedIn = auth.CurrentUser != null;
        if (!signedIn) {
            if (user == null)
                Debug.Log("FirebaseManager: no Firebase user signed in");
            else
                Debug.LogFormat("FirebaseManager: Firebase user {0} signed out", user.UserId);
            user = auth.CurrentUser; /* null */
        } else {
            user = auth.CurrentUser;
            Debug.LogFormat("FirebaseManager: User signed in successfully: {0} ({1})", user.DisplayName, user.UserId);

            StartCoroutine(Success());
        }
    }

    IEnumerator Success()
    {
        string scene = UIManager.instance.getCurrentScene();
        if (scene == UIManager.LOGIN_SCENE) {
            LoginUIController.instance.Login_WarningText.text = "";
            LoginUIController.instance.Login_ConfirmText.text = "Logged in!";
            yield return new WaitForSeconds(1);
            UIManager.instance.Goto_MainScene();
        } else if (scene == UIManager.REGISTER_SCENE) {
            yield return GetUserToken();
            RegisterUIController.instance.Register_WarningText.text = "";
            RegisterUIController.instance.Register_ConfirmText.text = "Registered!";
            RegisterUIController.instance.Register_NextButton.gameObject.SetActive(true);
        }

        yield break;
    }

    // Track ID token changes.
    void IdTokenChanged(object sender, System.EventArgs eventArgs) {
        /* Assumes one FirebaseAuth */
        FirebaseAuth senderAuth = sender as FirebaseAuth;
        if (senderAuth.CurrentUser != null) {
            senderAuth.CurrentUser.TokenAsync(false).ContinueWithOnMainThread(task => userToken = task.Result);
        }
    }

    public void OnClickLogin(string email, string password){
        // must use coroutine or else cannnot update the gameobjects on main thread (button)
        StartCoroutine(Login(email, password));
    }

    public void OnClickRegister(string email, string password, string confirmPassword){

        userToken = null;

        if (email == ""){
            RegisterUIController.instance.Register_WarningText.text = "Missing email address!";
            return;
        }

        if(password != confirmPassword){
            RegisterUIController.instance.Register_WarningText.text = "Password does not match!";
            return;
        }

        StartCoroutine(Register(email, password));
    }

    private IEnumerator GetUserToken(){
        if (user == null) {
            Debug.Log("FirebaseManager: Not signed in, unable to get token.");
            yield break;
        }
        Debug.LogFormat("Getting token for user: {0}", user.UserId);

        var task = user.TokenAsync(true);
        yield return new WaitUntil(predicate: () => task.IsCompleted);
        if (task.IsCanceled) {
            Debug.LogError("FirebaseManager: TokenAsync was canceled.");
            // return;
        }
        else if (task.IsFaulted) {
            Debug.LogError("FirebaseManager: TokenAsync encountered an error: " + task.Exception);
            // return;
        }
        else{
            userToken = task.Result;
            Debug.LogFormat("FirebaseManager: Got token: {0}", userToken);
        }
    }

    public void OnClickResetPassword(string email, string confirmEmail){
        if (email == ""){
            ResetPasswordUIController.instance.ResetPassword_WarningText.text = "Missing email address!";
            return;
        }

        if (email != confirmEmail){
            ResetPasswordUIController.instance.ResetPassword_WarningText.text = "Email addresses don't match!";
            return;
        }

        StartCoroutine(ResetPassword(email));
    }

    private IEnumerator Login(string email, string password){

        var LoginTask = auth.SignInWithEmailAndPasswordAsync(email, password);

        //Wait until the task completes
        yield return new WaitUntil(predicate: () => LoginTask.IsCompleted);

        if (LoginTask.Exception != null){
            //If there are errors handle them
            Debug.LogError("FirebaseManager: SignInWithEmailAndPasswordAsync encountered an error: " + LoginTask.Exception);
            FirebaseException firebaseEx = LoginTask.Exception.GetBaseException() as FirebaseException;
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

            string message = "Login Failed.";
            switch (errorCode){
                case AuthError.MissingEmail:
                    message = "Missing email.";
                    break;
                case AuthError.MissingPassword:
                    message = "Missing password.";
                    break;
                case AuthError.WrongPassword:
                    message = "Wrong password.";
                    break;
                case AuthError.InvalidEmail:
                    message = "Invalid email.";
                    break;
                case AuthError.UserNotFound:
                    message = "Account does not exist.";
                    break;
            }
            LoginUIController.instance.Login_WarningText.text = message;
        }
    }

    private IEnumerator Register(string email, string password){
        var RegisterTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);

        //Wait until the task completes
        yield return new WaitUntil(predicate: () => RegisterTask.IsCompleted);

        if (RegisterTask.IsFaulted) {
            Debug.LogError("FirebaseManager: CreateUserWithEmailAndPasswordAsync encountered an error: " + RegisterTask.Exception);
            FirebaseException firebaseEx = RegisterTask.Exception.GetBaseException() as FirebaseException;
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

            string message = "Register failed.";
            switch (errorCode)
            {
                case AuthError.MissingEmail:
                    message = "Missing email.";
                    break;
                case AuthError.MissingPassword:
                    message = "Missing password.";
                    break;
                case AuthError.WeakPassword:
                    message = "Weak password.";
                    break;
                case AuthError.EmailAlreadyInUse:
                    message = "Email already in use.";
                    break;
            }
            RegisterUIController.instance.Register_WarningText.text = message;
        } else {
            StartCoroutine(UserManager.instance.AddUser());
        }
    }

    private IEnumerator ResetPassword(string email){
        var task = auth.SendPasswordResetEmailAsync(email);

        //Wait until the task completes
        yield return new WaitUntil(predicate: () => task.IsCompleted);

        if (task.Exception != null) {
            Debug.LogError("CreateUserWithEmailAndPasswordAsync encountered an error: " + task.Exception);
            FirebaseException firebaseEx = task.Exception.GetBaseException() as FirebaseException;
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

            ResetPasswordUIController.instance.ResetPassword_WarningText.text = "Failed to send password reset email, please try again.";

        }else{
            ResetPasswordUIController.instance.ResetPassword_WarningText.text = "";
            ResetPasswordUIController.instance.ResetPassword_ConfirmText.text = "Password reset email sent, please check your email!";

            yield return new WaitForSeconds(2); // wait for 2s

            // go back to login screen
            UIManager.instance.Goto_LoginScene();
        }
    }

    ////////////////////////////////////////////////////////////////////////
    /////////////////////////////// Storage ////////////////////////////////
    ////////////////////////////////////////////////////////////////////////

    private IEnumerator getDownloadUrl(StorageReference sRef){
        var task = sRef.GetDownloadUrlAsync();
        yield return new WaitUntil(predicate: () => task.IsCompleted);

        if (task.Exception != null){
            Debug.Log(task.Exception.ToString());
        }else{
            downloadUrl = task.Result;
        }
    }

    public IEnumerator UploadFile(String localFile, String collection, String refName = null){

        if (refName == null){
            refName = FirebaseManager.instance.user.UserId;
        }

        String fileExt = Path.GetExtension(localFile);
        String refPath = collection + '/' + refName + fileExt;
        Debug.LogFormat("FirebaseManager: uploading file to reference path '{0}'", refPath);
        StorageReference sRef = storageRef.Child(refPath);


        var task = sRef.PutFileAsync(localFile);
        yield return new WaitUntil(predicate: () => task.IsCompleted);

        if (task.Exception != null){
            Debug.Log(task.Exception.ToString());
        }else{
            // Metadata contains file metadata such as size, content-type, and download URL.
            StorageMetadata metadata = task.Result;
            Debug.Log("FirebaseManager: finished uploading " + metadata.Md5Hash);

            yield return getDownloadUrl(sRef);
        }
    }

    public IEnumerator UploadBytes(byte[] bytes, String collection, String refName){

        String refPath = collection + '/' + refName;
        Debug.LogFormat("FirebaseManager: uploading bytes to reference path '{0}'", refPath);
        StorageReference sRef = storageRef.Child(refPath);

        var task = sRef.PutBytesAsync(bytes);
        yield return new WaitUntil(predicate: () => task.IsCompleted);

        if (task.Exception != null){
            Debug.Log(task.Exception.ToString());
        }else{
            // Metadata contains file metadata such as size, content-type, and download URL.
            StorageMetadata metadata = task.Result;
            Debug.Log("FirebaseManager: finished uploading " + metadata.Md5Hash);

            yield return getDownloadUrl(sRef);
        }
    }

    public IEnumerator DeleteFile(String url){
        Debug.LogFormat("FirebaseManager: deleting {0}", url);
        StorageReference sRef = storage.GetReferenceFromUrl(url);

        var task = sRef.DeleteAsync();
        yield return new WaitUntil(predicate: () => task.IsCompleted);

        if (task.Exception != null){
            Debug.Log(task.Exception.ToString());
        }else{
            Debug.LogFormat("FirebaseManager: successfully deleted from cloud storage: {0}", url);
        }
    }

    ////////////////////////////////////////////////////////////////////////

    void OnDestroy() {
      if (auth != null) {
        auth.StateChanged -= AuthStateChanged;
        auth.IdTokenChanged -= IdTokenChanged;
        auth = null;
      }
    }

}
