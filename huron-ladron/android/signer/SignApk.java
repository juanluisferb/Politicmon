import com.android.apksig.ApkSigner;
import com.android.apksig.ApkVerifier;
import java.io.File;
import java.io.FileInputStream;
import java.security.KeyStore;
import java.security.PrivateKey;
import java.security.cert.X509Certificate;
import java.util.Collections;

/** Firma (v2) y verifica un APK con apksig. Uso: SignApk in.apk out.apk keystore.p12 alias pass minSdk */
public class SignApk {
    public static void main(String[] a) throws Exception {
        File in = new File(a[0]), out = new File(a[1]);
        char[] pass = a[4].toCharArray();
        KeyStore ks = KeyStore.getInstance("PKCS12");
        try (FileInputStream f = new FileInputStream(a[2])) { ks.load(f, pass); }
        PrivateKey key = (PrivateKey) ks.getKey(a[3], pass);
        X509Certificate cert = (X509Certificate) ks.getCertificate(a[3]);
        int minSdk = Integer.parseInt(a[5]);

        ApkSigner.SignerConfig cfg = new ApkSigner.SignerConfig.Builder("CERT", key,
                Collections.singletonList(cert)).build();
        new ApkSigner.Builder(Collections.singletonList(cfg))
                .setInputApk(in).setOutputApk(out).setMinSdkVersion(minSdk)
                .setV1SigningEnabled(false).setV2SigningEnabled(true)
                .build().sign();

        ApkVerifier.Result r = new ApkVerifier.Builder(out).setMinCheckedPlatformVersion(minSdk).build().verify();
        for (ApkVerifier.IssueWithParams e : r.getErrors()) System.err.println("ERROR: " + e);
        for (ApkVerifier.IssueWithParams w : r.getWarnings()) System.err.println("AVISO: " + w);
        System.out.println("Firma verificada: " + r.isVerified() + " (v1=" + r.isVerifiedUsingV1Scheme()
                + ", v2=" + r.isVerifiedUsingV2Scheme() + ")");
        if (!r.isVerified()) System.exit(1);
    }
}
