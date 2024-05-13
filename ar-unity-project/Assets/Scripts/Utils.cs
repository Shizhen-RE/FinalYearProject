using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class Utils : MonoBehaviour
{
    private static Texture2D ImageTexture;
    private static string ImagePath;

    // prompt user to pick an image from album
    public static IEnumerator<(string, Texture2D)> PickImageFromAlbum(){
        Debug.Log("Pick image from album");
        NativeGallery.Permission per = NativeGallery.CheckPermission( NativeGallery.PermissionType.Read );

        // request for Read permission if not already granted
        if (per ==  NativeGallery.Permission.ShouldAsk){
            per = NativeGallery.RequestPermission( NativeGallery.PermissionType.Read );
        }

        if (per ==  NativeGallery.Permission.Granted){
            Debug.Log ("Read permission granted, picking image from gallery");

            bool done = false;
            NativeGallery.GetImageFromGallery((path) => {
                done = true; /* In case of exception */
                if (path != null){
                    // Create Texture from selected image
                    Debug.Log ("Image path: " + path);
                    ImageTexture = NativeGallery.LoadImageAtPath(path, 2073600, false);
                    ImagePath = path;
                }
            }, "Select an image", "image/*");

            while (!done)
                yield return (null, null);

            yield return (ImagePath, ImageTexture);
            yield break;
        }

        yield return (null, null);
    }

    public static Texture2D LoadTextureFromBytes(float width, float height, byte[] rawTextureByteArray, string format){
        Debug.LogFormat("Utils: loading texture format {0}", format);
        Texture2D texture = new Texture2D((int)width, (int)height, Utils.ConvertTextureFormat(format), false);
        texture.LoadRawTextureData(rawTextureByteArray);
        texture.Apply();
        Debug.LogFormat("Utils: loaded texture {0}x{1}", texture.width, texture.height);
        return texture;
    }

    public static TextureFormat ConvertTextureFormat(string textureFormatString){
        switch(textureFormatString){
            case "RGB24":
                return TextureFormat.RGB24;
            case "RGBA32":
                return TextureFormat.RGBA32;
            case "ARGB32":
                return TextureFormat.ARGB32;
            default:
                return TextureFormat.RGB24;
        }
    }

    public static Texture2D ScaleTexture(Texture2D source,int targetWidth,int targetHeight) {
        Texture2D result=new Texture2D(targetWidth,targetHeight,source.format,false);
        float incX=(1.0f / (float)targetWidth);
        float incY=(1.0f / (float)targetHeight);
        for (int i = 0; i < result.height; ++i) {
            for (int j = 0; j < result.width; ++j) {
                Color newColor = source.GetPixelBilinear((float)j / (float)result.width, (float)i / (float)result.height);
                result.SetPixel(j, i, newColor);
            }
        }
        result.Apply();
        return result;
    }

    // get city name from geographical coordinate for display
    public static string GetRegionFromCoordinates(double lat, double lon){
        //TODO: get region name from location

        return "Waterloo,ON,CA";
    }

    public static string GetCurrentRegionName(){
        double lat = ARContentManager.instance.latitude;
        double lon = ARContentManager.instance.longitude;

        return GetRegionFromCoordinates(lat, lon);
    }
}
