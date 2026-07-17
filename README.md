# تحديث الحماية التصاعدية لتسجيل الدخول

هذا التحديث يطبق:

- بعد 5 محاولات كلمة مرور فاشلة: تعليق لمدة دقيقة.
- الخطأ التالي بعد انتهاء التعليق: دقيقتان.
- ثم 5 دقائق.
- ثم 15 دقيقة للمحاولات اللاحقة.
- تصفير العداد بعد نجاح كلمة المرور.
- تصفير التصعيد إذا انقضت 30 دقيقة بعد انتهاء آخر تعليق دون محاولة فاشلة جديدة.
- حد IP منفصل لكل مسار مصادقة بدل مشاركة حد واحد بين الدخول وOTP.
- صفحات GET مثل فتح صفحة الدخول لا تدخل ضمن حد المصادقة.
- صفحة 429 رسمية مع عداد بدل الشاشة السوداء.
- OTP صالح 10 دقائق، بحد 5 محاولات تحقق.
- إعادة إرسال OTP بعد 60 ثانية، وبحد 3 عمليات إرسال خلال 10 دقائق.

## التركيب الآمن

1. أوقف النظام باستخدام Ctrl + C.
2. فك الضغط في مجلد مؤقت.
3. من داخل مجلد التحديث شغّل:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File ".\install.ps1" `
  -ProjectRoot "D:\KafoDotNet\Kafo.Web"
```

السكربت يحفظ نسخة احتياطية من كل ملف سيستبدله، ولا يلمس:

- `kafo.db`
- `appsettings.json`
- `appsettings.Development.json`
- `wwwroot/uploads`
- ملفات المستخدمين

## البناء

```powershell
cd D:\KafoDotNet\Kafo.Web

dotnet clean
Remove-Item .\bin, .\obj -Recurse -Force -ErrorAction SilentlyContinue
dotnet restore
dotnet build
```

ثم:

```powershell
dotnet run
```

## الإعدادات الاختيارية

القيم الافتراضية مدمجة في `SecurityOptions.cs`، لذلك لا يلزم تعديل الإعدادات. للتخصيص فقط، أضف القيم الموجودة في `appsettings.Security.Progressive.sample.json` إلى قسم `Security` الحالي دون استبدال بقية الملف.

## ملاحظة

لا توجد Migration جديدة ولا تغيير في مخطط قاعدة البيانات؛ التحديث يستخدم الحقول الموجودة أصلًا:

- `AccessFailedCount`
- `LockoutEndUtc`

## التراجع السريع

عند الحاجة للعودة إلى الملفات السابقة:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File ".\rollback-latest.ps1" `
  -ProjectRoot "D:\KafoDotNet\Kafo.Web"
```
