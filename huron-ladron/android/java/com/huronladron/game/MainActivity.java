package com.huronladron.game;

import android.app.Activity;
import android.graphics.Color;
import android.os.Build;
import android.os.Bundle;
import android.view.View;
import android.view.Window;
import android.view.WindowManager;
import android.webkit.WebChromeClient;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;

/**
 * Contenedor del juego: un WebView a pantalla completa que carga assets/index.html.
 * Se compila contra la API 16, así que lo posterior (inmersivo, recortes de pantalla,
 * audio sin gesto) se usa con constantes o reflexión.
 */
public class MainActivity extends Activity {
    private static final String EXIT_TITLE = "__exit__";
    private WebView web;

    @Override
    protected void onCreate(Bundle state) {
        super.onCreate(state);
        requestWindowFeature(Window.FEATURE_NO_TITLE);
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN
                | WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        allowDrawingInCutout();

        web = new WebView(this);
        web.setBackgroundColor(Color.BLACK);
        WebSettings s = web.getSettings();
        s.setJavaScriptEnabled(true);
        s.setDomStorageEnabled(true);
        s.setAllowFileAccess(true);
        invoke(s, "setMediaPlaybackRequiresUserGesture", false);
        web.setWebViewClient(new WebViewClient());
        web.setWebChromeClient(new WebChromeClient() {
            @Override
            public void onReceivedTitle(WebView view, String title) {
                // El juego pide salir cambiando el título del documento.
                if (EXIT_TITLE.equals(title)) finish();
            }
        });
        setContentView(web);
        hideSystemUi();
        web.loadUrl("file:///android_asset/index.html");
    }

    @Override
    public void onWindowFocusChanged(boolean hasFocus) {
        super.onWindowFocusChanged(hasFocus);
        if (hasFocus) hideSystemUi();
    }

    @Override
    public void onBackPressed() {
        // El botón atrás lo gestiona el juego (pausa, volver al menú, salir).
        web.loadUrl("javascript:window.__androidBack&&window.__androidBack()");
    }

    @Override
    protected void onPause() {
        super.onPause();
        web.onPause();
        web.pauseTimers();
    }

    @Override
    protected void onResume() {
        super.onResume();
        web.resumeTimers();
        web.onResume();
        hideSystemUi();
    }

    @Override
    protected void onDestroy() {
        if (web != null) web.destroy();
        super.onDestroy();
    }

    private void hideSystemUi() {
        // LAYOUT_STABLE | LAYOUT_HIDE_NAVIGATION | LAYOUT_FULLSCREEN | HIDE_NAVIGATION
        // | FULLSCREEN | IMMERSIVE_STICKY (0x1000, API 19)
        int flags = 0x100 | 0x200 | 0x400 | View.SYSTEM_UI_FLAG_HIDE_NAVIGATION | 0x4;
        if (Build.VERSION.SDK_INT >= 19) flags |= 0x1000;
        getWindow().getDecorView().setSystemUiVisibility(flags);
    }

    private void allowDrawingInCutout() {
        if (Build.VERSION.SDK_INT < 28) return;
        try {
            WindowManager.LayoutParams lp = getWindow().getAttributes();
            lp.getClass().getField("layoutInDisplayCutoutMode").setInt(lp, 1); // SHORT_EDGES
            getWindow().setAttributes(lp);
        } catch (Exception ignored) {
        }
    }

    private static void invoke(Object target, String method, boolean value) {
        try {
            target.getClass().getMethod(method, boolean.class).invoke(target, value);
        } catch (Exception ignored) {
        }
    }
}
