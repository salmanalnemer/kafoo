# قائمة نشر آمن — Kafo

## قبل البناء

- تثبيت SDK المحدد في `global.json`.
- تشغيل `scripts/security-check.ps1` أو `scripts/security-check.sh` ومعالجة أي فشل.
- التأكد من عدم وجود قاعدة بيانات أو ملفات مرفوعة أو أسرار داخل المصدر.
- مراجعة نتيجة `dotnet list package --vulnerable --include-transitive`.

## إعداد حساب الخدمة والمسارات

```bash
sudo useradd --system --home /var/lib/kafo --shell /usr/sbin/nologin kafo
sudo install -d -o kafo -g kafo -m 0750 /var/lib/kafo
sudo install -d -o kafo -g kafo -m 0750 /var/lib/kafo/secure-uploads
sudo install -d -o kafo -g kafo -m 0700 /var/lib/kafo/data-protection-keys
sudo install -d -o kafo -g kafo -m 0750 /var/log/kafo
sudo install -d -o root -g kafo -m 0750 /etc/kafo
sudo install -o root -g kafo -m 0640 deploy/kafo.env.example /etc/kafo/kafo.env
```

عدّل `/etc/kafo/kafo.env` ولا تستخدم القيم النموذجية كما هي.

## ClamAV

- تثبيت `clamav-daemon` وتحديث التواقيع.
- تفعيل TCP socket على `127.0.0.1:3310` فقط، أو استخدام شبكة داخلية موثوقة.
- اختبار الخدمة قبل تشغيل التطبيق.
- اترك `MalwareScanning__FailClosed=true` في الإنتاج.

## قاعدة البيانات والنسخ الاحتياطي

- استخدم مسارًا خارج مجلد الإصدار مثل `/var/lib/kafo/kafo.db`.
- خذ نسخة احتياطية مشفرة قبل كل ترقية.
- اختبر الاستعادة دوريًا.
- لا تمنح مستخدم خدمة التطبيق صلاحيات أكثر من القراءة/الكتابة للمسارات المطلوبة.

## أول تشغيل

- عيّن `BootstrapAdmin__*` فقط إذا كانت قاعدة البيانات فارغة.
- شغّل التطبيق؛ الترحيلات تطبق تلقائيًا.
- سجّل الدخول وغيّر كلمة المرور إذا طُلب.
- احذف جميع متغيرات `BootstrapAdmin__*` ثم أعد تشغيل الخدمة.

## Reverse Proxy وTLS

- استخدم شهادة TLS صحيحة وتحويل HTTP إلى HTTPS.
- حدّث `AllowedHosts` و`Security__PublicBaseUrl` و`Email__Notifications__PublicBaseUrl` للنطاق الفعلي.
- أضف فقط IP الوكيل الحقيقي داخل `ReverseProxy__KnownProxies`.
- لا تعرض منفذ Kestrel على الإنترنت؛ اربطه بـ `127.0.0.1` أو شبكة داخلية.

## بعد النشر

- اختبر تسجيل دخول الإدارة والداعم والجهة وOTP والقفل المؤقت.
- اختبر أن رابط إعداد كلمة المرور يعمل مرة واحدة وينتهي.
- جرّب رفع ملف سليم وملف اختبار EICAR في بيئة معزولة للتأكد من الرفض.
- تحقق أن `/uploads/job-applications-cv/...` يعيد 404 وأن `/secure-files/...` يتطلب الصلاحية.
- افحص CSP والترويسات وHTTPS من خارج الخادم.
- نفّذ فحص DAST واختبار اختراق على Staging قبل اعتماد الإنتاج.
- راقب `/var/log/kafo` وسجلات النظام والتنبيهات الأمنية.
