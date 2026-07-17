# إعداد بريد OTP

تم ضبط النظام لبيانات البريد الظاهرة في لوحة الاستضافة:

- البريد واسم المستخدم: `support@kafoo.org.sa`
- خادم SMTP: `mail.kafoo.org.sa`
- المنفذ: `465`
- الحماية: SSL/TLS ضمني من بداية الاتصال (`SslOnConnect`)
- المصادقة: اسم المستخدم وكلمة مرور صندوق البريد

> ملاحظة: هذا الحساب ينتهي بـ `.org.sa` وليس `.com.sa`.

## 1. حفظ كلمة المرور محليًا

من داخل مجلد المشروع نفّذ:

```powershell
dotnet user-secrets set "Email:Smtp:Password" "كلمة-مرور-البريد-الفعلية"
```

يمكن تثبيت بقية الإعدادات صراحة في User Secrets أيضًا:

```powershell
dotnet user-secrets set "Email:Smtp:Host" "mail.kafoo.org.sa"
dotnet user-secrets set "Email:Smtp:Port" "465"
dotnet user-secrets set "Email:Smtp:EnableSsl" "true"
dotnet user-secrets set "Email:Smtp:UserName" "support@kafoo.org.sa"
dotnet user-secrets set "Email:Smtp:FromEmail" "support@kafoo.org.sa"
```

تحقق من القيم المحفوظة:

```powershell
dotnet user-secrets list
```

لا تضع كلمة المرور داخل `appsettings.json` ولا ترفعها إلى Git.

## 2. استعادة المكتبات وبناء المشروع

تم استبدال `System.Net.Mail.SmtpClient` بـ `MailKit` لأن المنفذ 465 يحتاج SSL ضمنيًا، وهو غير مدعوم بالطريقة المطلوبة في `System.Net.Mail.SmtpClient`.

```powershell
dotnet restore
dotnet clean
dotnet build
dotnet run --launch-profile http
```

## 3. اختبار الاتصال من Windows

```powershell
Test-NetConnection mail.kafoo.org.sa -Port 465
```

يجب أن تكون النتيجة:

```text
TcpTestSucceeded : True
```

إذا ظهرت `False` فالمشكلة من DNS أو الجدار الناري أو شركة الاستضافة، وليست من رمز OTP.

## 4. عند استمرار خطأ المصادقة

تأكد من الآتي:

1. كلمة المرور هي كلمة مرور صندوق البريد `support@kafoo.org.sa` وليست كلمة مرور لوحة الاستضافة.
2. يمكن تسجيل الدخول إلى Webmail بالحساب نفسه.
3. لا توجد مسافة زائدة عند نسخ كلمة المرور.
4. خدمة SMTP Authentication مفعّلة، كما يظهر في لوحة الاستضافة.
5. أعد تشغيل التطبيق بعد تعديل User Secrets.

## 5. قراءة سبب الخطأ الحقيقي

شغّل النظام من PowerShell ولا تغلق النافذة. عند فشل الإرسال سيظهر في السجل نوع الخطأ الحقيقي، مثل:

- `Authentication failed`: اسم المستخدم أو كلمة المرور غير صحيحة.
- `Connection refused` أو `SocketException`: الخادم أو المنفذ محجوب.
- `SSL handshake failed`: مشكلة شهادة أو اسم خادم غير مطابق.
- `Mailbox unavailable`: عنوان المرسل أو المستلم مرفوض.
