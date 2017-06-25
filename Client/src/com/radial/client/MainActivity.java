package com.radial.client;
import android.app.Activity;
import android.content.Intent;
import android.graphics.Bitmap;
import android.os.Bundle;
import android.view.View;
import android.view.View.OnClickListener;
import android.util.Log;
import android.os.StrictMode;
import android.net.Uri;
import android.provider.MediaStore;
import android.content.Context;
import android.database.Cursor;
import android.os.Environment;
import android.content.ContentResolver;
import android.content.SharedPreferences;
import android.widget.*;
import java.net.*;
import java.io.*;

public class MainActivity extends Activity {
    /* Defaults and statics */
    public static final String PREFS_NAME = "TCPClientConf";
    public static final int DEFAULT_QUALITY = 80;
    public static final int DEFAULT_DIM = 512;
    public static final int DEFAULT_PORT = 3800;
    private static final int CAMERA_REQUEST = 1888;

    /* Declare Controls */
    private EditText IPEditText;
    private EditText QualityEditText;
    private EditText DimensionEditText;
    private EditText PortEditText;
    
    private Uri mImageUri;

    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.main);

        /* Initialize Controls */
        Button photoButton = (Button) this.findViewById(R.id.button1);
        Button saveButton = (Button) this.findViewById(R.id.button2);
        IPEditText = (EditText)findViewById(R.id.txtip);
        QualityEditText = (EditText)findViewById(R.id.txtquality);
        DimensionEditText = (EditText)findViewById(R.id.txtdim);
        PortEditText = (EditText)findViewById(R.id.txtport);

        /* Load Preferences */
        final SharedPreferences settings = getSharedPreferences(PREFS_NAME, 0);     
        IPEditText.setText(settings.getString("IP", ""));
        QualityEditText.setText(Integer.toString(settings.getInt("Quality", DEFAULT_QUALITY)));
        DimensionEditText.setText(Integer.toString(settings.getInt("Dimension", DEFAULT_DIM)));
        PortEditText.setText(Integer.toString(settings.getInt("Port", DEFAULT_PORT)));

        photoButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                /* Check if server is up */
                boolean ServerAlive = serverListening(settings.getString("IP", ""),settings.getInt("Port", DEFAULT_PORT));
                if(ServerAlive){
                    Intent intent = new Intent("android.media.action.IMAGE_CAPTURE");
                    File photo;
                    try
                    {
                        /* Place where to store camera taken picture */
                        photo = createTemporaryFile("picture", ".jpg");
                        photo.delete();
                    }
                    catch(Exception e)
                    {
                        Toast.makeText(getApplicationContext(), "Please check SD card! Image shot is impossible!", 1000).show();
                        return;
                    }
                    mImageUri = Uri.fromFile(photo);
                    intent.putExtra(MediaStore.EXTRA_OUTPUT, mImageUri);
                    //Start Camera Intent
                    startActivityForResult(intent, CAMERA_REQUEST);
                    }           
            }
        });

        saveButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                SharedPreferences settings = getSharedPreferences(PREFS_NAME, 0);
                SharedPreferences.Editor editor = settings.edit();
                try{
                    /* Check for erroneous values */
                    int Qual=Integer.parseInt(QualityEditText.getText().toString());
                    int Dim=Integer.parseInt(DimensionEditText.getText().toString());
                    int Port=Integer.parseInt(PortEditText.getText().toString());
                    if (Qual > 100 || Qual < 0) throw new Exception("Invalid Quality Value");
                    if (Dim < 0) throw new Exception("Invalid Dimension Value");
                    if (Port <= 0 || Port >= 65534) throw new Exception("Invalid Port");
                    
                    /* Save Preferences */
                    editor.putInt("Quality", Qual);
                    editor.putInt("Dimension", Dim);
                    editor.putInt("Port", Port);
                    editor.putString("IP", IPEditText.getText().toString());
                }
                catch(Exception e){ 
                    Toast.makeText(getApplicationContext(),e.getMessage(),Toast.LENGTH_SHORT).show();
                    return;
                }
                /* Commit the edits! */
                editor.commit();
                Toast.makeText(getApplicationContext(), "Preferences Saved", 1000).show();
            }
        });
        
    }
    
    /* Create temporary file to test permissions */
    public File createTemporaryFile(String part, String ext) throws Exception
    {
        File tempDir= Environment.getExternalStorageDirectory();
        tempDir=new File(tempDir.getAbsolutePath()+"/.temp/");
        if(!tempDir.exists())
        {
            tempDir.mkdirs();
        }
        return File.createTempFile(part, ext, tempDir);
    }
    
    /* Get the bitmap of the image */
    public Bitmap grabImage()
    {
        this.getContentResolver().notifyChange(mImageUri, null);
        ContentResolver cr = this.getContentResolver();
        Bitmap bitmap;
        try
        {
            bitmap = android.provider.MediaStore.Images.Media.getBitmap(cr, mImageUri);
            return bitmap;
        }
        catch (Exception e)
        {
            Toast.makeText(this, "Failed to load", Toast.LENGTH_SHORT).show();
        }
        return null;
    }

    /* After camera activity closes */
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {  
        if (requestCode == CAMERA_REQUEST && resultCode == Activity.RESULT_OK) {
            
            /* Load Preferences */
            SharedPreferences settings = getSharedPreferences(PREFS_NAME, 0);
            final int ImageDim = settings.getInt("Dimension", DEFAULT_DIM);
            
            /* Scale down and send the image */
            try
            {
                runJavaSocket(scaleDown(grabImage(), ImageDim, true));
            }
            catch (Exception e)
            {
                Toast.makeText(getApplicationContext(),e.getMessage(),Toast.LENGTH_SHORT).show();
            }
            
            super.onActivityResult(requestCode, resultCode, data);
        }
    } 
    
    protected void runJavaSocket(Bitmap bmp) {
        /* Load Preferences */
        SharedPreferences settings = getSharedPreferences(PREFS_NAME, 0);
        final String IP = settings.getString("IP", "");
        final int ImageQuality = settings.getInt("Quality", DEFAULT_QUALITY);
        
        /* Allow network operations on main thread. NOTE: This is a bad practice! */
        StrictMode.ThreadPolicy policy = new StrictMode.ThreadPolicy.Builder().permitAll().build();
        StrictMode.setThreadPolicy(policy);
        
        /* Check if server was killed while clicking */
        boolean ServerAlive = serverListening(IP, settings.getInt("Port", DEFAULT_PORT));
        if (!ServerAlive) return;
        
        /* Compress Image */
        ByteArrayOutputStream stream = new ByteArrayOutputStream();
        bmp.compress(Bitmap.CompressFormat.JPEG, ImageQuality, stream);
        ByteArrayInputStream rdr = new ByteArrayInputStream(stream.toByteArray());
        
        /* Adjust buffer size here */
        byte[] buffer = new byte[1024];

    try{
        /* Open a connection */
        Socket socket = new Socket(InetAddress.getByName(IP), settings.getInt("Port", DEFAULT_PORT));

        /* Write everything */
        OutputStream output = socket.getOutputStream();
        int count;
        while ((count = rdr.read(buffer,0,buffer.length)) > 0) {
           output.write(buffer, 0, count);
        }
        
        /* Flush the output to commit */
        output.flush();
    }
    catch (Exception e){
        Toast.makeText(getApplicationContext(),e.getMessage(),Toast.LENGTH_SHORT).show();
        Log.e("MYAPP", "exception", e);
    }
    }
    
    /* Scales down a bitmap keeping aspect ratio to have one dimension as indicated */
    public static Bitmap scaleDown(Bitmap realImage, float maxImageSize,
        boolean filter) {
        float ratio = Math.min(
                (float) maxImageSize / realImage.getWidth(),
                (float) maxImageSize / realImage.getHeight());
        int width = Math.round((float) ratio * realImage.getWidth());
        int height = Math.round((float) ratio * realImage.getHeight());
    
        Bitmap newBitmap = Bitmap.createScaledBitmap(realImage, width,
                height, filter);
        return newBitmap;
    }
    
    /* Check if IP:Port is open */
    public boolean serverListening(String host, int port)
    {
        StrictMode.ThreadPolicy policy = new StrictMode.ThreadPolicy.Builder().permitAll().build();
        StrictMode.setThreadPolicy(policy);
        Socket s = new Socket();
        try
        {
            s.connect(new InetSocketAddress(host, port), 1000);
            return true;
        }
        catch (Exception e)
        {
            Toast.makeText(getApplicationContext(),e.getMessage(),Toast.LENGTH_SHORT).show();
            return false;
        }
        finally
        {
            if(s != null)
                try {s.close();}
                catch(Exception e){}
        }
    }
    
}