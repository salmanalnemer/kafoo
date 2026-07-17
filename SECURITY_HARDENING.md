# Kafo Security-Hardened Source

هذه النسخة مصدر نظيف للنظام، ولا تحتوي على قاعدة بيانات أو ملفات مستخدمين أو نسخ احتياطية أو أسرار تشغيل.

## الإصلاحات المطبقة

- إزالة كلمة مرور المدير الافتراضية من المصدر. إنشاء المدير الأول يتم من متغيرات بيئة مؤقتة فقط.
- استخدام `ASP.NET Core Identity PasswordHasher` مع 600,000 دورة، ودعم ترقية التجزئة القديمة تلقائيًا بعد تسجيل الدخول الصحيح.
- سياسة كلمة مرور من 12 إلى 128 حرفًا، ومنع كلمات المرور الشائعة، وإلزام اختلاف كلمة المرور الجديدة.
- قفل الحساب بعد المحاولات الفاشلة، وRate Limiting لتسجيل الدخول وOTP والنماذج العامة.
- OTP محفوظ في قاعدة البيانات ومشفّر بـ Data Protection، محدود حسب البريد وIP، وينتهي ويستخدم مرة واحدة.
- إلغاء إرسال كلمات المرور المؤقتة. الاستبدال برابط HTTPS أحادي الاستخدام ومحدود الصلاحية.
- Security Stamp للتحقق من الجلسة في كل طلب، وإبطال الجلسات عند تغيير كلمة المرور أو البريد أو حالة الحساب.
- تقليل مدة الجلسة، تعطيل Sliding Expiration، واستخدام Cookies آمنة `HttpOnly/Secure/SameSite=Strict`.
- نقل السير الذاتية والتقارير والمرفقات الخاصة خارج `wwwroot`، وتنزيلها فقط من Controller يتحقق من الملكية والصلاحية.
- حظر مسارات الملفات الخاصة القديمة داخل `/uploads`، مع خدمة ترحيل تلقائي للملفات القديمة عند توفرها في بيئة الترقية.
- التحقق من الامتداد وMIME والتوقيع الحقيقي للملف وحد الحجم، مع فحص ClamAV قبل الحفظ.
- تفعيل ClamAV بوضع Fail-Closed في الإنتاج.
- إزالة `Html.Raw` و`innerHTML` ومعالجات JavaScript المضمنة، وإضافة CSP تعتمد على nonce.
- رفض الروابط غير الآمنة والسماح فقط بمسار محلي آمن أو رابط HTTPS حسب نوع الحقل.
- ترويسات CSP وHSTS وnosniff وframe protection وReferrer Policy وPermissions Policy.
- Anti-forgery عام لطلبات تغيير الحالة.
- سجل تدقيق أمني لطلبات التغيير والدخول والتنزيلات الخاصة، وسجل ملفات يومي محدود الاحتفاظ.
- ضبط AllowedHosts وPublicBaseUrl وForwarded Headers للوكلاء الموثوقين.
- فحوصات تلقائية داخل `scripts/` وGitHub Actions، مع NuGet audit.

## التشغيل المحلي

```powershell
$env:Email__Smtp__Password = "YOUR_SMTP_PASSWORD"
$env:BootstrapAdmin__FullName = "مدير النظام"
$env:BootstrapAdmin__UserName = "admin"
$env:BootstrapAdmin__Email = "admin@example.org"
$env:BootstrapAdmin__Password = "A-Strong-One-Time-Password!2026"

dotnet restore
dotnet run
```

بعد إنشاء أول مدير، احذف متغيرات `BootstrapAdmin__*` فورًا. لا تحفظها داخل `appsettings.json`.

## الفحص قبل النشر

Windows:

```powershell
.\scripts\security-check.ps1
```

Linux:

```bash
./scripts/security-check.sh
```

## ملاحظات مهمة

- لا تحفظ قاعدة البيانات أو `App_Data` أو `wwwroot/uploads` داخل Git أو ZIP المصدر.
- احتفظ بمفاتيح Data Protection في مسار ثابت محمي؛ فقدانها يبطل OTP والجلسات والبيانات المحمية.
- يجب أن يعمل ClamAV TCP daemon على العنوان المحدد، وإلا سيرفض النظام رفع الملفات في الإنتاج.
- إعدادات الخادم وTLS والجدار الناري والنسخ الاحتياطية واختبار الاختراق الديناميكي مسؤوليات تشغيلية ولا يمكن ضمانها من الكود وحده.

## Build validation correction — 2026-07-17

- Corrected `StartsWith` calls that combined a `char` argument with `StringComparison`; they now use the string overload.
- Added a direct `SQLitePCLRaw.bundle_e_sqlite3` 2.1.12 reference to prevent restoring the vulnerable 2.1.11 native SQLite package.
- Hardened `scripts/security-check.ps1` so every native command exit code is checked and a failed build can no longer be reported as successful.
