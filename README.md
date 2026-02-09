#  Proje Özeti  
## End-to-End Sistem Akışı

Bu proje, **Unity tabanlı bir mobil uygulama** için **güvenli**, **ölçeklenebilir** ve **sürdürülebilir** bir **kullanıcı kimlik doğrulama ve profil yönetim altyapısı** sunar.

Kullanıcılar:

- **Email / Şifre**
- **Google Sign-In**

yöntemleriyle giriş yapabilir.  
Giriş yapan kullanıcıların temel bilgileri **Firebase Firestore** üzerinde **kalıcı ve güvenli** şekilde saklanır.

Sistem; **UI**, **Authentication**, **Routing** ve **Database** katmanlarının **birbirinden ayrıldığı**, ancak **kontrollü şekilde birlikte çalıştığı** bir mimari üzerine kuruludur.

---

##  Kullanılan Teknolojiler

- **Unity 6.0.58f2 LTS**
- **C#**
- **Firebase Authentication**
- **Firebase Firestore**
- **Google Sign-In**
- **Async / Await** *(asenkron işlemler)*

## Firestore Database Security Rule
- Please Copy and Paste this code block into Firestore Rules
```
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {

    match /users/{userId} {

      // Read: only the owner
      allow read: if isOwner(userId);

      // Create: first login only, strict schema
      allow create: if isOwner(userId)
        && request.resource.data.keys().hasOnly([
          "uid",
          "email",
          "displayName",
          "photoUrl",
          "createdAt",
          "lastLoginAt"
        ])
        && request.resource.data.uid == userId
        && request.resource.data.email == request.auth.token.email
        && request.resource.data.displayName is string
        && request.resource.data.photoUrl is string
        && request.resource.data.createdAt is timestamp
        && request.resource.data.lastLoginAt is timestamp;

      // Update: only allowed fields may change
      allow update: if isOwner(userId)
        && request.resource.data.keys().hasOnly([
          "uid",
          "email",
          "displayName",
          "photoUrl",
          "createdAt",
          "lastLoginAt"
        ])
        // Immutable fields
        && request.resource.data.uid == resource.data.uid
        && request.resource.data.email == resource.data.email
        && request.resource.data.createdAt == resource.data.createdAt;

      // Never allow client-side deletes
      allow delete: if false;
    }
  }

  function isOwner(userId) {
    return request.auth != null
      && request.auth.uid == userId;
  }
}

```

---

## Sahne Yapısı ve Genel Akış

Uygulama **3 ana sahneden** oluşur:

- **Bootstrap Scene**
- **Login Scene**
- **Profile Scene**

> Sahne geçişleri **kullanıcı state’ine göre otomatik** olarak gerçekleştirilir.

============================================================

## 1-) Uygulama Başlangıcı  
### Bootstrap Scene

Uygulama açıldığında ilk olarak **Bootstrap Scene** yüklenir.

Bu sahnede:

- Firebase bağımlılıkları kontrol edilir
- Firebase Authentication başlatılır
- Kullanıcının daha önce oturum açıp açmadığı belirlenir
- **Remember Me** tercihi değerlendirilir

### Olası Senaryolar

- **Firebase başlatılamazsa**
  - Kullanıcıya hata popup’ı gösterilir
  - *Retry* veya *uygulamayı kapatma* seçeneği sunulur

- **Kullanıcı giriş yapmamışsa**
  - → **Login Scene**’e yönlendirilir

- **Kullanıcı giriş yapmışsa**
  - → Auth state doğrulanır
  - → Profil kontrol sürecine geçilir

> NOTE: **FirebaseBootstrapper Scripti**, uygulama genelinde **tek yetkili yönlendirme noktasıdır**.

---

## 2-) Kimlik Doğrulama  
### Login Scene

**Login Scene**, kullanıcıdan giriş veya kayıt bilgilerini alan **UI katmanıdır**.

Bu sahne:

-  İş logic **içermez**
-  Sadece:
  - `AuthService`
  - `GoogleAuthService`
- tetikler
- Hataları **PopupService** üzerinden kullanıcıya gösterir

### Desteklenen Giriş Yöntemleri

#### Email / Şifre ile Giriş
- UI tarafında input doğrulama yapılır
- Firebase Authentication üzerinden giriş yapılır
- Başarılı giriş sonrası routing **Bootstrap** tarafından yapılır

#### Email / Şifre ile Kayıt
- Firebase üzerinde kullanıcı oluşturulur
- **Display Name** set edilir
- Doğrulama email’i gönderilir
- Kullanıcı email doğrulaması yapana kadar **otomatik çıkış** yaptırılır

#### Google Sign-In
- Google hesap seçimi yapılır
- **Google IdToken** alınır
- Firebase credential exchange gerçekleştirilir
- Kullanıcı Firebase Authentication’a giriş yapmış olur

---

## 3-) Profil Oluşturma & Güncelleme  
### Database Logic

Kullanıcı başarıyla authenticate olduktan sonra, **Profile Scene’e geçilmeden önce** veritabanı kontrolü yapılır.

### İlk Giriş (Register)

Firestore’da `users/{uid}` dokümanı **yoksa**:

Aşağıdaki alanlarla yeni bir kullanıcı profili oluşturulur:

- `uid`
- `email`
- `displayName`
- `photoURL`
- `createdAt`
- `lastLoginAt`

### Tekrar Giriş (Login)

Mevcut kullanıcı için:

- Sadece `lastLoginAt` güncellenir
- Kullanıcı tarafından düzenlenebilir alanlar **korunur**

> Tüm bu süreç **tamamen `UserProfileRepository` üzerinden** yürütülür.

---

## 4-) Profile Scene  
### Profil Görüntüleme & Düzenleme

**Profile Scene**, kullanıcının kalıcı verilerinin gösterildiği ve **sınırlı düzenleme** yapabildiği alandır.

Bu sahnede:

- Kullanıcı bilgileri Firestore’dan çekilir
- **Display Name** düzenlenebilir
- Profil fotoğrafı *(varsa URL üzerinden)* yüklenir
- Aşağıdaki bilgiler **salt okunur** olarak gösterilir:
  - Email
  - Hesap oluşturma tarihi
  - Son giriş tarihi

###  Logout

- FirebaseAuth üzerinden çıkış yapılır
- Google Sign-In kullanılmışsa **Google session** temizlenir
- Routing otomatik olarak **Login Scene**’e döner

>  **Profile UI**, asla sahne değiştirme kararı vermez.

---

## 5-) Veritabanı Güvenliği  
### Firestore Security Rules

Firestore Security Rules sayesinde:

- Kullanıcı **sadece kendi verisini** okuyabilir
- Sadece **izin verilen alanlar** yazılabilir
- Aşağıdaki alanlar **değiştirilemez**:
  - `uid`
  - `email`
  - `createdAt`
- Client tarafında **silme tamamen engellenmiştir**

> Bu kurallar, backend logic ile **birebir uyumlu** çalışır ve  
> istemci tarafındaki hataları **güvenlik riski haline getirmez**.

------------------------------------------------------------
------------------------------------------------------------

# Başlangıç (Bootstrap) Logic

Uygulama açıldığında, tüm sistemlerin **güvenli** ve **tutarlı** bir şekilde başlatılması için **Bootstrap Scene** kullanılır.  
Bu sahne, Firebase servislerinin hazırlanmasından ve kullanıcının hangi sahneye yönlendirileceğinin belirlenmesinden sorumludur.

Bu süreç **iki ana bileşen** üzerinden yürütülür:

1-) AppServices: Persistent servis container  
2-) FirebaseBootstrapper: Başlangıç ve yönlendirme otoritesi  

------------------------------------------------------------

## AppServices – Persistent Servis Yapısı

AppServices, sahne değişimlerinden etkilenmeyen ve uygulama boyunca yaşayan **persistent** servisleri barındırır.

- Singleton pattern ile çalışır
- Sahne geçişlerinde yok edilmez
- PopupService, LoadingService gibi servislerin uygulama boyunca tek instance olarak kalmasını sağlar

```csharp
private void Awake()
{
    if (Instance != null)
    {
        Destroy(gameObject);
        return;
    }

    Instance = this;
    DontDestroyOnLoad(gameObject);
}
```
------------------------------------------------------------

## FirebaseBootstrapper – Uygulama Başlangıç Akışı

FirebaseBootstrapper, uygulama açılır açılmaz çalışan ve **tek yetkili yönlendirme mekanizması** olan sınıftır.

### Firebase Başlatma Akışı

- Firebase başlatma sürecini yürütür
- Dependency kontrolünü yapar
- Authentication state değişimlerini izler
- Kullanıcıyı Login veya Profile sahnesine yönlendirir
- Firebase başlatma işlemi **async** olarak tetiklenir
- UI thread bloklanmaz
- Ağ ve servis işlemleri arka planda yürütülür 
```
private async void Start()
{
    await InitializeFirebaseAsync();
}
```
------------------------------------------------------------

## Firebase Dependency Kontrolü

Dependency kontrolü:

- Firebase’in cihazda çalışabilmesi için gerekli native bağımlılıkları kontrol eder
- Eksik veya bozuk bağımlılıkları otomatik düzeltmeye çalışır
- İşlem tamamlanana kadar metot bekler
```
DependencyStatus dependencyStatus =
    await FirebaseApp.CheckAndFixDependenciesAsync();
```
------------------------------------------------------------

## Dependency Başarısız Olursa

Dependency uygun değilse:

- Uygulama ilerlemez
- Kullanıcıya tam ekran bir hata popup’ı gösterilir
- Kullanıcıdan aksiyon alınana kadar sistem güvenli state’te kalır
```
if (dependencyStatus != DependencyStatus.Available)
{
    ShowConnectionErrorPopup();
    return;
}
```
------------------------------------------------------------

## PopupService ile Hata Yönetimi

PopupService:

- Tüm sahnelerde erişilebilir persistent bir servistir
- Kullanıcıdan aktif aksiyon bekleyen durumlarda kullanılır

Popup üzerindeki butonlar:

1-) Confirm → Firebase başlatma süreci tekrar denenir  
2-) Cancel → Uygulama kapatılır  

Bu yaklaşım:

- Sessiz hataları engeller
- Kullanıcıyı belirsiz bir state’te bırakmaz
```
PopupService.Instance.ShowConfirmation(
    PopupType.Error,
    "Connection Error",
    "Unable to connect to server...",
    OnRetryConnection,
    OnQuitApplication
);
```
------------------------------------------------------------

## Authentication State Takibi

FirebaseAuth state değişimleri sürekli izlenir:

- Giriş
- Çıkış
- Token yenileme
- Oturum restore edilmesi
```
firebaseAuth.StateChanged += OnAuthStateChanged;
```


State change handler içinde:
- Sistem initialize değilse ignore edilir
- Aynı kullanıcı için tekrar routing yapılması bilinçli olarak engellenir
- Kullanıcı değiştiğinde routing tetiklenir
```
private void OnAuthStateChanged(object sender, EventArgs eventArgs)
{
    if (!isInitialized)
        return;

    if (currentUser == previousUser)
        return;

    previousUser = currentUser;
    RouteUserAsync();
}
```
------------------------------------------------------------

## Kullanıcı Yönlendirme (RouteUserAsync)

RouteUserAsync:

- Async çalışır
- Aynı anda birden fazla kez çağrılmasını engelleyen `isRouting` guard’ına sahiptir
- Kullanıcının state’ine göre Login veya Profile sahnesini yükler
```
private async void RouteUserAsync()
```
------------------------------------------------------------

## Cold Start & Remember Me Kontrolü

Bu kontrol sadece uygulamanın ilk açılışında çalışır.

Remember Me kapalıysa:

- Firebase session aktif olsa bile kullanıcı çıkış yaptırılır
- Login sahnesine yönlendirilir
```
if (isColdStart)
{
    isColdStart = false;

    if (!RememberMeUtility.IsRememberMeEnabled())
    {
        firebaseAuth.SignOut();
        LoadSceneIfNeeded("Login");
        return;
    }
}
```
------------------------------------------------------------

## Auth Kontrolü

Aktif kullanıcı yoksa:

- Uygulama ilerlemez
- Login sahnesine yönlendirilir
```
if (currentUser == null)
{
    LoadSceneIfNeeded("Login");
    return;
}
```
------------------------------------------------------------

## Kullanıcı Verisini Yenileme

Kullanıcı verisi Firebase’ten yeniden çekilir:

- Email doğrulama durumu gibi bilgilerin stale olmasını engeller
- En güncel auth state ile karar verilir
```
await currentUser.ReloadAsync();
```
------------------------------------------------------------

## Kritik Hata Durumu

Auth reload sürecinde hata oluşursa:

- Geçici network hataları tolere edilir
- Auth state bozulduysa güvenli şekilde çıkış yapılır
- Login sahnesine geri dönülür
```
catch (FirebaseException ex)
{
    if (ex.ErrorCode != (int)AuthError.NetworkRequestFailed)
    {
        firebaseAuth.SignOut();
        LoadSceneIfNeeded("Login");
        return;
    }
}
```
------------------------------------------------------------

## Email Doğrulama Akışı

Email/Password ile giriş yapan kullanıcı için email doğrulaması kontrol edilir.

Email doğrulanmamışsa:

1-) Popup ile kullanıcıya doğrulama maili gönderme seçeneği sunulur  
2-) Firebase üzerinden doğrulama maili async gönderilir  
3-) UI thread bloklanmaz  
```
if (isEmailPasswordUser && !currentUser.IsEmailVerified)
```
```
await currentUser.SendEmailVerificationAsync();
```
------------------------------------------------------------

## Profil Kontrolü ve Güncelleme

Kullanıcı authenticate olduktan sonra Firestore profili kontrol edilir:

- İlk girişse profil oluşturulur
- Mevcut kullanıcıysa lastLoginAt / LastLoginDate güncellenir
```
await profileRepository.EnsureProfileExistsAndUpdateLoginAsync();
```
------------------------------------------------------------

## Son Sahne Yönlendirmesi

Tüm kontroller başarıyla tamamlandıysa:

- Kullanıcı Profile sahnesine yönlendirilir
```
LoadSceneIfNeeded("Profile");
```
------------------------------------------------------------

## Özet

Bootstrap Logic:

- Firebase’i güvenli şekilde başlatır
- Auth state’i sürekli izler
- Hataları kullanıcıya görünür şekilde bildirir
- Yanlış veya yarım state’leri engeller
- Uygulamayı deterministik ve ölçeklenebilir hale getirir


============================================================

# Authentication Logic

Bu projede kimlik doğrulama işlemleri, **Firebase Authentication** üzerine kurulmuş **merkezi servisler** aracılığıyla yönetilir.  
Amaç; giriş, kayıt ve üçüncü parti sağlayıcı (**Google**) işlemlerinin **tek bir doğruluk kaynağından**, **kontrollü** ve **UI’dan bağımsız** şekilde yürütülmesidir.

> Bu katman **asla UI kararı vermez**.  
> Sadece *ne olduğunu* bildirir, *ne yapılacağını* söylemez.

Authentication sistemi **üç ana yapıdan** oluşur:

1-) **AuthService** (Email / Password)  
2-) **GoogleAuthService** (Google Sign-In)  
3-) **AuthSessionContext** (Geçici akış durumu)  

------------------------------------------------------------


## AuthService – Email / Password Authentication

AuthService, Firebase Authentication ile **doğrudan konuşan** ve  
email / şifre tabanlı tüm işlemleri yöneten **servis katmanıdır**.

Bu servis:

- UI içermez  
- Popup göstermez  
- Hataları **event olarak yayınlar**  
- UI katmanının nasıl tepki vereceğine **karar vermez**

> Bu tasarım, **authentication logic** ile **UI**’yı tamamen ayrıştırır.

------------------------------------------------------------

## Email ile Giriş (Sign In)

Email / şifre ile giriş süreci async olarak yürütülür.
```
public async Task SignInWithEmailAsync(string email, string password)
{
    try
    {
        AuthResult result =
            await firebaseAuth.SignInWithEmailAndPasswordAsync(email, password);
    }
    catch (Exception exception)
    {
        HandleAuthException(exception);
    }
}
```
### Async Akış Açıklaması

- Firebase’e ağ üzerinden giriş isteği gönderilir  
- UI thread bloklanmaz  
- İşlem tamamlanana kadar metot bekler  

### Başarılı Olursa

- Firebase **CurrentUser** state’ini günceller  
- Bootstrap sahnesindeki **StateChanged listener** tetiklenir  
- Kullanıcı yönlendirme kararı **Bootstrap** tarafından verilir  

### Hata Olursa

- Exception yakalanır  
- Hata tipi merkezi handler üzerinden analiz edilir  
- UI katmanına **event** ile bildirilir  

------------------------------------------------------------

## Email ile Kayıt (Sign Up)

Email ile kayıt süreci Firebase üzerinde yeni kullanıcı oluşturur.
```
AuthResult result =
    await firebaseAuth.CreateUserWithEmailAndPasswordAsync(email, password);
```
Bu işlem:

- Firebase üzerinde yeni kullanıcı oluşturur  
- Email adresinin daha önce kullanılıp kullanılmadığını kontrol eder  

------------------------------------------------------------

## Display Name Ayarlama

Kayıt sonrası kullanıcının görünen ismi Firebase Auth profiline yazılır.
```
await result.User.UpdateUserProfileAsync(profile);
```
Bu bilgi:

- Firebase Auth üzerinde saklanır  
- Google veya diğer provider’larla **tutarlı** kalır  

------------------------------------------------------------

## Email Doğrulama Gönderimi

Kayıt sonrası kullanıcıya doğrulama email’i gönderilir.
```
await result.User.SendEmailVerificationAsync();
```
Bu aşamada:

- Firebase üzerinden doğrulama maili gönderilir  
- Kullanıcıdan emailini doğrulaması beklenir  

------------------------------------------------------------

## Zorunlu Çıkış (Post-Signup)

Email doğrulama sonrası kullanıcı **bilinçli olarak çıkış yaptırılır**.
```
firebaseAuth.SignOut();
```
Bu yaklaşım:

- Doğrulanmamış kullanıcıların sisteme girmesini engeller  
- Bootstrap logic, doğrulama yapılmadan Profile sahnesine geçmez  

------------------------------------------------------------

## Şifre Sıfırlama

Kullanıcı şifresini unuttuğunda Firebase üzerinden reset maili gönderilir.
```
await firebaseAuth.SendPasswordResetEmailAsync(email);
```
Bu işlemden sonra:

- UI kullanıcıyı bilgilendirir  
- AuthService herhangi bir popup göstermez  

------------------------------------------------------------

## Merkezi Hata Yönetimi

Firebase kaynaklı hatalar **tek bir noktada** ele alınır.
```
AuthError authError = (AuthError)firebaseException.ErrorCode;
MapFirebaseError(authError);
```
```
case AuthError.EmailAlreadyInUse:
    RaiseError(
        AuthErrorType.EmailAlreadyInUse,
        "This email is already in use.");
    break;
```
Bu yapıda:

- Firebase’in düşük seviyeli hata kodları  
- Uygulama seviyesinde **anlamlı kategorilere** dönüştürülür  

Bu yaklaşım:

- Firebase bağımlılığını UI’dan gizler  
- UI’nın hata metinlerini **tam kontrol etmesini** sağlar  

------------------------------------------------------------

## UI Entegrasyonu (PopupService)

AuthService, hata oluştuğunda sadece **event fırlatır**.
```
OnAuthError?.Invoke(errorType, message);
```
UI katmanı:
```
PopupService.Instance.ShowError(title, message);
```
- Bu event’e subscribe olur  
- PopupService üzerinden kullanıcıya geri bildirim verir  

> Authentication servisi popup göstermez,  
> sadece **ne olduğunu söyler**.

------------------------------------------------------------


## AuthSessionContext – Akış Farkındalığı

AuthSessionContext, authentication sürecindeki **geçici state’leri** tutar.
```
public static bool IsSignUpInProgress { get; private set; }
```
Bu yapı:

- Signup sırasında geçici olarak aktif edilir  
- Bootstrap routing logic’ine  
  “şu an signup akışındayız” bilgisini verir  

Amaç:

- Email doğrulama popup’larının  
- Signup UI akışıyla çakışmasını engellemek  

------------------------------------------------------------

## GoogleAuthService – Google Sign-In Akışı

Google Sign-In, Firebase Authentication ile  
**credential exchange** yapılarak gerçekleştirilir.

Bu servis:

- Singleton ve persistent’tır  
- FirebaseBootstrapper’dan bağımsız çalışır  
- Hataları event olarak yayınlar  

------------------------------------------------------------

## Google Sign-In Başlatma

Google hesap seçimi async olarak başlatılır.
```
GoogleSignInUser googleUser =
    await GoogleSignIn.DefaultInstance.SignIn();
```
Bu işlem:

- Kullanıcıya Google hesap seçtirir  
- İşlem iptal edilirse null dönebilir  

------------------------------------------------------------

## Token Doğrulama

Google’dan dönen IdToken kontrol edilir.
```
if (string.IsNullOrEmpty(googleUser.IdToken))
```
Bu kontrol:

- Credential güvenliğini sağlar  
- IdToken yoksa Firebase’e geçilmez  

------------------------------------------------------------

## Firebase ile Kimlik Değişimi

Google kimliği Firebase credential’a dönüştürülür.
```
Credential credential =
    GoogleAuthProvider.GetCredential(googleUser.IdToken, null);

await firebaseAuth.SignInWithCredentialAsync(credential);
```
Bu aşamada:

- Firebase user session başlatılır  
- Bootstrap routing tekrar devreye girer  

------------------------------------------------------------

## Google Session Temizleme

Google oturumu işlem sonrası temizlenir.
```
GoogleSignIn.DefaultInstance.SignOut();
GoogleSignIn.DefaultInstance.Disconnect();
```
Bu yaklaşım:

- Google hesabının cache’lenmesini engeller  
- Bir sonraki girişte kullanıcıdan tekrar hesap seçmesini sağlar  

------------------------------------------------------------

## Google Hata Yönetimi

Google ve Firebase kaynaklı tüm hatalar:

- Tek tip event mekanizmasıyla UI’ya iletilir  
- PopupService üzerinden kullanıcıya gösterilir  
```
OnAuthError?.Invoke(errorType, message);
```
------------------------------------------------------------

## Özet

Authentication Logic:

- Email / Password ve Google girişlerini merkezi şekilde yönetir  
- Async / Await ile UI donmalarını engeller  
- Firebase hatalarını uygulama seviyesinde normalize eder  
- UI’dan tamamen bağımsızdır  
- Bootstrap routing ile entegre çalışır  

============================================================

# Database Logic  
## Firestore Kullanıcı Profili Yönetimi

Bu projede kullanıcıya ait **kalıcı veriler**, **Firebase Firestore** üzerinde saklanır.  
Veritabanı yapısı, **Firebase Authentication** ile **doğrudan ilişkili** olacak şekilde tasarlanmıştır.

> Authentication **kim olduğumuzu**,  
> Firestore ise **bizimle ilgili kalıcı bilgileri** bilir.

Her kullanıcı için Firestore’da **tek bir belge** bulunur:

- `users/{uid}`

Buradaki `{uid}`, Firebase Authentication tarafından üretilen **benzersiz kullanıcı ID’sidir**.

------------------------------------------------------------

## UserProfileData – Firestore Veri Modeli

**Script:** `UserProfileData.cs`

Bu sınıf, Firestore’da saklanan kullanıcı verisinin  
**strongly-typed** modelidir.
```
[FirestoreData]
public sealed class UserProfileData
{
    [FirestoreProperty] public string uid { get; set; }
    [FirestoreProperty] public string email { get; set; }
    [FirestoreProperty] public string displayName { get; set; }
    [FirestoreProperty] public string photoUrl { get; set; }
    [FirestoreProperty] public Timestamp createdAt { get; set; }
    [FirestoreProperty] public Timestamp lastLoginAt { get; set; }
}
```
### Amaç

- Firestore ↔ Unity veri dönüşümünü **otomatikleştirmek**
- JSON parse hatalarını **engellemek**
- Profile UI logic’te **direkt kullanılabilir** bir model sunmak

============================================================

## UserProfileRepository – Veritabanı Erişim Katmanı

**Script:** `UserProfileRepository.cs`

Bu sınıf, Firestore ile ilgili **tüm okuma / yazma işlemlerinin tek sorumlusudur**.

Bu repository:

- UI içermez  
- Scene bilmez  
- Sadece  
  “veri doğru mu?”,  
  “eksik mi?”,  
  “güncellendi mi?”  
  sorularına odaklanır  

> Bu yapı **Repository Pattern** uygular ve  
> Firestore erişimini tek merkezde toplar.

------------------------------------------------------------

## Profil Oluşturma & Giriş Güncelleme Akışı

### Çağırıldığı Yer

- **Script:** FirebaseBootstrapper  
- **Method:** RouteUserAsync  
```
await profileRepository.EnsureProfileExistsAndUpdateLoginAsync();
```
Bu çağrı:

- Kullanıcı **başarılı şekilde authenticate olduktan sonra**
- **Profile sahnesine geçmeden hemen önce** yapılır

============================================================

## EnsureProfileExistsAndUpdateLoginAsync()

**Script:** UserProfileRepository  
**Method:** EnsureProfileExistsAndUpdateLoginAsync
```
DocumentSnapshot snapshot =
    await docRef.GetSnapshotAsync();
```
Bu `await`:

- Firestore’a ağ üzerinden istek atar  
- `users/{uid}` belgesinin **var olup olmadığını** kontrol eder  
- UI thread’i **bloklamaz**

------------------------------------------------------------

## İlk Giriş (Register Senaryosu)

Eğer Firestore belgesi **yoksa**:

- Kullanıcı **ilk kez giriş yapıyor** demektir
- FirebaseAuth’tan alınan verilerle Firestore dokümanı oluşturulur
- `createdAt` alanı **server timestamp** ile set edilir
```
if (!snapshot.Exists)
{
    updates["uid"] = user.UserId;
    updates["email"] = user.Email;
    updates["createdAt"] = FieldValue.ServerTimestamp;
    updates["displayName"] = user.DisplayName;
    updates["photoUrl"] = user.PhotoUrl;
}
```
> ServerTimestamp kullanımı,  
> client taraflı zaman manipülasyonlarını engeller.

------------------------------------------------------------

## Mevcut Kullanıcı (Login Senaryosu)

Belge **zaten varsa**:

- Kullanıcının düzenleyebileceği alanlar **overwrite edilmez**
- Sadece `lastLoginAt` alanı güncellenir
```
Dictionary<string, object> updates = new()
{
    { "lastLoginAt", FieldValue.ServerTimestamp }
};
```
Bu yaklaşım:

- Profil bilgilerinin yanlışlıkla sıfırlanmasını önler
- Audit / analytics için **güvenilir login verisi** sağlar

------------------------------------------------------------

## Yazma İşlemi (Create / Update)
```
await docRef.SetAsync(updates, SetOptions.MergeAll);
```
Bu `await`:

- Firestore dokümanını **oluşturur veya günceller**
- Sadece gönderilen alanları etkiler
- Mevcut alanları **korur**

------------------------------------------------------------

## Profil Okuma Akışı

### Çağırıldığı Yer

- **Script:** Profile UI Logic  
- **Method:** LoadProfile  
```
DocumentSnapshot snapshot =
    await docRef.GetSnapshotAsync();
```
Bu akışta:

- Firestore’dan kullanıcı profili çekilir
- Belge yoksa `null` döner
- UI bu duruma göre **fallback** gösterebilir
```
return snapshot.ConvertTo<UserProfileData>();
```
> Firestore JSON → UserProfileData dönüşümü  
> **otomatik** yapılır.

------------------------------------------------------------


## Profil Güncelleme – Display Name

**Script:** UserProfileRepository  
**Method:** UpdateDisplayNameAsync

Bu işlem **iki adımda** yapılır:

1-) Firebase Authentication profilinin güncellenmesi  
2-) Firestore’daki kullanıcı belgesinin güncellenmesi  
```
await user.UpdateUserProfileAsync(authProfile);
```
```
await docRef.UpdateAsync("displayName", trimmedName);
```
> Bu iki adım **bilinçli olarak ayrılmıştır**.  
> Auth ve Database tutarsızlığı engellenir.

------------------------------------------------------------


## Profil Güncelleme – Photo URL

**Script:** UserProfileRepository  
**Method:** UpdatePhotoUrlAsync

Google Sign-In kullanıcıları için:

- Fotoğraf **yüklenmez**
- Sadece **URL saklanır**
```
await user.UpdateUserProfileAsync(authProfile);
await docRef.UpdateAsync("photoUrl", photoUrl);
```
------------------------------------------------------------

## Hata Yönetimi

Tüm Firestore işlemleri **try / catch** ile korunur.
```
catch (FirebaseException ex)
{
    if (ex.ErrorCode == 7)
        Debug.LogError("Permission denied by Firestore rules");
    throw;
}
```
Bu yaklaşım:

- Permission hatalarını debug ortamında açıkça loglar
- Exception’ı yukarı fırlatır
- UI veya Bootstrap bu hatayı yakalayarak  
  **PopupService ile kullanıcıyı bilgilendirir**

------------------------------------------------------------


## Güvenlik Tasarımı (Özet)

Bu veritabanı yapısı:

- Kullanıcıları **UID ile izole eder**
- Authentication olmadan erişimi **engeller**
- ServerTimestamp kullanarak client manipülasyonunu **önler**
- Firestore Rules ile güvence altına alınmaya **uygundur**

> Firestore katmanı,  
> Auth logic ile **senkron**,  
> UI ile **tamamen ayrık** çalışır.

============================================================

# Login Logic  
## Login UI & Authentication Flow

Login Logic, kullanıcının uygulamaya giriş yapabildiği **tüm akışları yöneten**,  
**UI merkezli kontrol katmanıdır**.

Bu katman, authentication işlemlerini **doğrudan kendisi yapmaz**;  
bunun yerine **AuthService** ve **GoogleAuthService** üzerinden tetikler.

Bu ayrım sayesinde:

- UI sade kalır  
- Authentication logic tekrar kullanılabilir olur  
- Test edilebilirlik artar  

> Login katmanı **ne yapılacağını söyler**,  
> **nasıl yapılacağını bilmez**.

------------------------------------------------------------

## LoginUIController – UI Orkestrasyonu

- **Script:** LoginUIController.cs  
- **Sahne:** Login  

LoginUIController:

- Login, Sign Up ve Forgot Password panellerini yönetir  
- Kullanıcı etkileşimlerini alır  
- Doğrulama ve geri bildirimleri **PopupService** üzerinden sağlar  

------------------------------------------------------------

## Servis Bağlantıları

### AuthService Bağlantısı

LoginUIController, Email / Password authentication işlemleri için  
**AuthService** ile doğrudan iletişim kurar.
```
private AuthService authService;
```
```
authService = new AuthService();
authService.OnAuthError += HandleAuthError;
```
Bu bağlantı:

- Email / Password authentication işlemlerini yönetir  
- Hataları **event** olarak UI’ya iletir  

------------------------------------------------------------

### GoogleAuthService Bağlantısı

LoginUIController, Google Sign-In işlemleri için  
**GoogleAuthService** ile iletişim kurar.
```
GoogleAuthService.Instance.OnAuthError += HandleAuthError;
```
Bu sayede:

- Google Sign-In kaynaklı hatalar  
- AuthService ile **aynı hata UI akışını** kullanır  

------------------------------------------------------------

## Panel Yönetimi (UI State)

Login sahnesindeki paneller yalnızca **UI state** değiştirir.

Panel switch metodları:

- `OpenSignIn()`  
  - Login panelini açar  
- `OpenSignUp()`  
  - Kayıt panelini açar  
- `OpenForgotPassword()`  
  - Şifre sıfırlama panelini açar  

> Bu metodlar **iş logic içermez**.

------------------------------------------------------------

## Email ile Giriş Akışı

### UI Entry Point

- **Script:** LoginUIController  
- **Method:** OnSignInClicked  
```
await authService.SignInWithEmailAsync(email, password);
```
------------------------------------------------------------

### Akış Adımları

1-) **Input Validation**
```
if (string.IsNullOrWhiteSpace(...))
```
- Boş veya geçersiz alanlar kontrol edilir  
- Firebase çağrılmadan önce temel doğrulama yapılır  
- Gereksiz ağ çağrıları engellenir  

2-) **Remember Me Kaydı**
```
RememberMeUtility.SetRememberMe(rememberMeToggle.isOn);
```
- Kullanıcının tercihi kaydedilir  
- Bu değer Bootstrap sahnesinde değerlendirilir  

3-) **Loading UI**
```
LoadingService.Instance.Show();
```
- Loading UI gösterilir  
- Kullanıcıya işlem devam ederken geri bildirim sağlanır  

4-) **AuthService → SignIn**
```
await authService.SignInWithEmailAsync(...)
```
- FirebaseAuth’a giriş isteği gönderilir  
- İşlem tamamlanana kadar beklenir  
- UI thread donmaz  

------------------------------------------------------------

### Giriş Başarılı Olursa

- FirebaseAuth state değişir  
- FirebaseBootstrapper.OnAuthStateChanged tetiklenir  
- FirebaseBootstrapper.RouteUserAsync çalışır  
- Kullanıcı Login sahnesinden çıkarılır  

> LoginUIController **sahne geçişi yapmaz**.  
> Bu karar yalnızca **Bootstrap logic**’e aittir.

------------------------------------------------------------

### Hata Olursa
```
PopupService.Instance.ShowError(
    "Authentication Error",
    message);
```
- AuthService → OnAuthError event’i tetikler  
- LoginUIController.HandleAuthError çalışır  
- PopupService üzerinden kullanıcı bilgilendirilir  

------------------------------------------------------------

## Google ile Giriş Akışı

### UI Entry Point

- **Script:** LoginUIController  
- **Method:** OnGoogleSignInClicked  
```
await GoogleAuthService.Instance.SignInWithGoogleAsync();
```
------------------------------------------------------------

### Akış Adımları

1-) Remember Me tercihi kaydedilir  
2-) Loading UI gösterilir  
3-) Google Sign-In başlatılır  
4-) Google hesap seçme ekranı açılır  
5-) IdToken alınır  
6-) Firebase credential exchange yapılır  

------------------------------------------------------------

### Başarılı Olursa

- FirebaseAuth.CurrentUser set edilir  
- Bootstrap routing devreye girer  
- Profile kontrolü başlatılır  

------------------------------------------------------------

## Email ile Kayıt (Sign Up) Akışı

### UI Entry Point

- **Script:** LoginUIController  
- **Method:** OnSignUpClicked  
```
bool success =
    await authService.SignUpWithEmailAsync(...);
```
------------------------------------------------------------

### Akış Adımları

1-) **UI Validation**

- Boş alanlar kontrol edilir  
- Şifre eşleşmesi doğrulanır  

2-) **AuthSessionContext**
```
AuthSessionContext.BeginSignUp();
```
- Signup süreci başlatılır  
- Bootstrap routing geçici olarak baskılanır  

3-) **AuthService → SignUp**
```
PopupService.Instance.ShowConfirmation(...)
```
- Firebase kullanıcı oluşturur  
- DisplayName set edilir  
- Doğrulama maili gönderilir  
- Kullanıcı zorunlu olarak sign-out edilir  

------------------------------------------------------------

### Başarılı Olursa

- PopupService ile kullanıcı bilgilendirilir  
- Login paneline yönlendirilir  
- Email doğrulanmadan girişe izin verilmez  

------------------------------------------------------------

## Şifre Sıfırlama Akışı

### UI Entry Point

- **Script:** LoginUIController  
- **Method:** OnForgotPasswordClicked  
```
await authService.SendPasswordResetEmailAsync(email);
```
------------------------------------------------------------

### Güvenlik Notu

Firebase:

- Email’in sistemde olup olmadığını **bilinçli olarak söylemez**

Bu yüzden UI:

- Her zaman “mail gönderildi” mesajı gösterir  

------------------------------------------------------------

## Hata Yönetimi (UI Katmanı)

### Merkezi Hata Yakalama

- Tüm AuthService ve GoogleAuthService hataları  
- Tek bir popup akışıyla gösterilir  
```
private void HandleAuthError(AuthErrorType type, string message)
```
------------------------------------------------------------

## Mimari Özet

Login Logic:

- UI ile Auth logic’i ayırır  
- Async / Await ile kullanıcı deneyimini korur  
- Sahne geçişi yapmaz  
- Tüm kararları Bootstrap’e bırakır  
- Hataları PopupService üzerinden kullanıcıya açıkça bildirir  

============================================================

# Profile UI Logic  
## Profil Görüntüleme ve Güncelleme

Profile UI Logic, giriş yapmış kullanıcının veritabanında saklanan **profil bilgilerini görüntülemek** ve  
**sınırlı düzenleme işlemlerini** (display name) gerçekleştirmekten sorumludur.

Bu katman:

- Authentication yapmaz  
- Routing yapmaz  
- Veritabanına **doğrudan değil**, `UserProfileRepository` üzerinden erişir  

> Bu yaklaşım sayesinde UI,  
> **iş logic’ten tamamen ayrılmış** olur.

------------------------------------------------------------

## ProfileUIController – Ana UI Kontrolcüsü

- **Script:** ProfileUIController.cs  
- **Sahne:** Profile  

ProfileUIController aşağıdaki sorumluluklara sahiptir:

1-) Profil verisini yüklemek  
2-) UI bileşenlerine bind etmek  
3-) Edit mode yönetmek  
4-) Profil fotoğrafını yüklemek  
5-) Logout işlemini tetiklemek  

------------------------------------------------------------

## Başlatma Akışı (Initialization)

### Entry Point

- **Script:** ProfileUIController  
- **Method:** Start  
```
_ = InitializeAsync();
```
Bu yaklaşım:

- UI thread’i bloklamaz  
- Sahne açılışını geciktirmez  

------------------------------------------------------------

### InitializeAsync()

Bu aşamada:

- Firestore repository instance’ı oluşturulur  
- Profil yükleme süreci başlatılır  
```
profileRepository = new UserProfileRepository();
await LoadProfileAsync();
```
------------------------------------------------------------

## Profil Yükleme Akışı

### LoadProfileAsync()

- **Script:** ProfileUIController  
- **Method:** LoadProfileAsync  
```
currentProfile =
    await profileRepository.GetCurrentUserProfileAsync();
```
Bu `await`:

- Firestore’a ağ isteği atar  
- `users/{uid}` dokümanını çeker  
- UI thread’in donmasını engeller  

------------------------------------------------------------

### Başarılı Olursa

- Profil verisi UI’ya bind edilir  
- Edit butonu aktif hale getirilir  
```
BindProfileToUI(currentProfile);
editButton.interactable = true;
```
------------------------------------------------------------

### Hata Olursa

Kullanıcıya hata popup’ı gösterilir ve iki seçenek sunulur:

- Retry → Profil tekrar yüklenir  
- Logout → Güvenli çıkış yapılır  
```
PopupService.Instance.ShowConfirmation(
    PopupType.Error,
    "Profile Error",
    "Failed to load your profile...",
    onConfirm: async () => await LoadProfileAsync(),
    onCancel: OnLogoutClicked
);
```
Bu tasarım:

- Kullanıcıya **retry şansı verir**  
- Veritabanı state’i bozuksa **güvenli çıkış** sağlar  

------------------------------------------------------------

## UI Binding (View Mode)

### BindProfileToUI()
```
displayNameText.text = profile.displayName;
emailText.text = profile.email;
```
Profil verisi UI bileşenlerine bind edilir:

- Display Name  
- Email  

Ayrıca aşağıdaki alanlar gösterilir:

- Hesap oluşturma tarihi  
- Son giriş tarihi  
```
createdDateText.text = FormatDate(profile.CreatedAtUtc);
lastLoginDateText.text = FormatDate(profile.LastLoginAtUtc);
```
> Tarihler **server timestamp** üzerinden gelir.  
> Client saat manipülasyonu etkisizdir.

------------------------------------------------------------

## Profil Fotoğrafı Yükleme

### Neden Coroutine?

Profil fotoğrafı için:

- Firebase Storage kullanılmaz  
- Sadece **URL üzerinden indirme** yapılır  

Bu nedenle:

- `UnityWebRequest`  
- `Coroutine`  

kullanımı tercih edilmiştir.

------------------------------------------------------------

### Fotoğraf Yoksa

- Default avatar kullanılır  
```
if (string.IsNullOrWhiteSpace(photoUrl))
{
    profileImage.sprite = defaultAvatarSprite;
    return;
}
```
------------------------------------------------------------

### Fotoğraf Yükleme

- Görsel asenkron olarak indirilir  
- UI donmaz  
```
yield return request.SendWebRequest();
```
Hata olursa:

- Default avatar fallback olarak kullanılır
```
profileImage.sprite = defaultAvatarSprite;
```

------------------------------------------------------------

## Edit Mode – Display Name Güncelleme

### Edit Mode Açma

- Mevcut display name input alanına yazılır  
- Edit paneli aktif edilir  
```
displayNameInput.text = currentProfile.displayName;
editPanel.SetActive(true);
```
------------------------------------------------------------

### Save İşlemi

- Kullanıcı yeni display name girer  
- Repository üzerinden update çağrılır  
```
await profileRepository.UpdateDisplayNameAsync(newDisplayName);
```
Bu çağrı:

- FirebaseAuth üzerindeki DisplayName’i günceller  
- Firestore `users/{uid}` dokümanını günceller  

> Bu **çift güncelleme**,  
> Auth ↔ Database tutarlılığını garanti eder.

------------------------------------------------------------

### Başarılı Olursa

- UI lokal state güncellenir  
- Edit paneli kapatılır  
```
currentProfile.displayName = newDisplayName;
displayNameText.text = newDisplayName;
editPanel.SetActive(false);
```
------------------------------------------------------------

### Hata Olursa

- Kullanıcı popup ile bilgilendirilir  
- UI eski state’te kalır  
```
PopupService.Instance.ShowError(
    "Update Failed",
    "Failed to update display name...");
```
> Veri tutarsızlığı oluşmaz.

------------------------------------------------------------

## Logout Akışı

### UI Entry Point

- **Method:** OnLogoutClicked  
```
FirebaseAuth.DefaultInstance.SignOut();
```
Bu çağrı:

- FirebaseAuth state’ini sıfırlar  
- Sahne değiştirmez  

------------------------------------------------------------

### Google Logout

- Google session temizlenir  
- Bir sonraki girişte account picker zorlanır  
```
GoogleAuthService.Instance.ClearGoogleSession();
```
============================================================

## Routing Neden Burada Yok?

Logout sonrası:

1-) FirebaseAuth state değişir  
2-) FirebaseBootstrapper.OnAuthStateChanged tetiklenir  
3-) FirebaseBootstrapper.RouteUserAsync çalışır  
4-) Kullanıcı otomatik olarak Login sahnesine yönlendirilir  

> Profile UI **routing kararı vermez**.

------------------------------------------------------------

## Mimari Özet

Profile UI Logic:

- Veriyi sadece Repository üzerinden alır  
- Async / Await + Coroutine’i bilinçli kullanır  
- Hataları PopupService ile kullanıcıya bildirir  
- Logout sonrası kontrolü Bootstrap’e bırakır  
- Clean UI / Logic separation uygular  

============================================================
============================================================

# Database Security Logic  
## Firestore Rules

Bu projede kullanıcı verilerinin güvenliği, **Firebase Firestore Security Rules** üzerinden sağlanır.  
Amaç; istemci tarafının **yalnızca izin verilen veriler üzerinde**, **sahibi olduğu kayıtlar için** işlem yapabilmesini garanti etmektir.

> Firestore Rules, **son savunma hattıdır**.  
> Client logic hatalı olsa bile veri güvenliği korunur.

Firestore kuralları, uygulama tarafındaki  
**UserProfileRepository logic’i ile birebir uyumlu** olacak şekilde tasarlanmıştır.

------------------------------------------------------------

## Kapsam ve Genel Yapı

Firestore güvenlik yapısı şu prensiplere dayanır:

- Rules **v2** kullanılır  
- Kurallar **doküman seviyesinde** çalışır  
- **Authentication zorunludur**  
- Anonymous veya public erişim yoktur  
```
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
```
------------------------------------------------------------

## users Koleksiyonu Koruması

Firestore’da her kullanıcı için **tek bir profil dokümanı** bulunur:

- Koleksiyon: `users`
- Doküman ID: `{uid}` (Firebase Authentication UID)
```
match /users/{userId} {
```
Bu tasarım:

- Veri izolasyonunu basitleştirir  
- Rule karmaşıklığını azaltır  
- UID spoofing riskini minimize eder  

------------------------------------------------------------

## Ownership Kontrolü

### isOwner Helper Function

Tüm güvenlik modelinin temeli **ownership** kontrolüdür.
```
function isOwner(userId) {
  return request.auth != null
    && request.auth.uid == userId;
}
```
Bu fonksiyon:

- Kullanıcının authenticated olup olmadığını kontrol eder  
- Sadece kendi UID’siyle eşleşen dokümana erişmesine izin verir  

> Read / Create / Update işlemlerinin **tamamı** bu kontrole dayanır.

------------------------------------------------------------

## Read Rule – Veri Okuma

- Kullanıcı **sadece kendi profilini** okuyabilir  
- Başka kullanıcıların verileri **tamamen kapalıdır**  
- Admin veya public read **yoktur**
```
allow read: if isOwner(userId);
```
Bu yapı:

- GDPR / KVKK uyumlu  
- Net ve izole bir veri modeli sağlar  

------------------------------------------------------------

## Create Rule – İlk Giriş (Register)

Create işlemi yalnızca:

- Kullanıcı authenticated ise  
- Kendi UID’sine ait dokümanı oluşturuyorsa  

mümkündür.
```
allow create: if isOwner(userId)
```
------------------------------------------------------------

### Şema Kısıtlaması (Schema Enforcement)

Create sırasında yalnızca izin verilen alanlar yazılabilir.
```
request.resource.data.keys().hasOnly([
  "uid",
  "email",
  "displayName",
  "photoUrl",
  "createdAt",
  "lastLoginAt"
])
```
Bu kısıtlama:

- Fazladan alan eklenmesini engeller  
- Client-side field injection saldırılarını kapatır  

Özellikle engellenen senaryolar:

- Role ekleme  
- Admin flag ekleme  
- Gizli alan sokma  

------------------------------------------------------------

### Veri Doğrulamaları

Create sırasında:

- UID client’tan değil, **path’ten** doğrulanır  
- Email, **JWT token** üzerinden kontrol edilir  
```
request.resource.data.uid == userId
request.resource.data.email == request.auth.token.email
```
Bu sayede:

- UID spoofing  
- Fake email yazma  

tamamen engellenir.

------------------------------------------------------------

### Timestamp Doğrulaması

- `createdAt` alanı **timestamp olmak zorundadır**  
- Client tarafından keyfi tarih gönderilemez  
```
request.resource.data.createdAt is timestamp
```
Bu yaklaşım:

- Tarih manipülasyonunu engeller  
- Audit verilerini güvenilir kılar  

------------------------------------------------------------

## Update Rule – Profil Güncelleme

Update işlemi:

- Sadece kullanıcının **kendi dokümanı** üzerinde yapılabilir  
```
allow update: if isOwner(userId)
```
------------------------------------------------------------

### Alan Kısıtlaması

Update sırasında da **aynı whitelist** kullanılır.
```
request.resource.data.keys().hasOnly([...])
```
Bu sayede:

- Yeni alan eklenemez  
- Şema dışı veri yazılamaz  

------------------------------------------------------------

### Immutable Alanlar

Aşağıdaki alanlar **asla değiştirilemez**:

- `uid`  
- `email`  
- `createdAt`  
```
request.resource.data.uid == resource.data.uid
request.resource.data.email == resource.data.email
request.resource.data.createdAt == resource.data.createdAt
```
> Bu kural, uygulama tarafındaki şu mantıkla birebir örtüşür:  
> Mevcut kullanıcılar için yalnızca  
> `displayName`, `photoUrl` ve `lastLoginAt` güncellenebilir.

------------------------------------------------------------

## Delete Rule – Silme

Client tarafında silme **tamamen yasaktır**.
```
allow delete: if false;
```
Bu yaklaşım:

- Veri kaybını önler  
- Kötü niyetli silme girişimlerini engeller  

Silme işlemleri:

- Sadece backend / admin yetkisiyle yapılabilir  

------------------------------------------------------------

## Backend Logic ile Birebir Uyum

Firestore Rules ↔ Backend Logic eşleşmesi:

- Merge update → Immutable alanlar korunur  
- ServerTimestamp → Timestamp doğrulaması  
- UID bazlı doc ID → Ownership kontrolü  
- Alan whitelist → Controlled update  

Bu uyum sayesinde:

> Client logic ne kadar hatalı olursa olsun,  
> Firestore **son savunma hattı** olarak çalışır.

------------------------------------------------------------

## Güvenlik Tasarımının Avantajları

Bu rules yapısı aşağıdaki açıkları **tamamen kapatır**:

- Yetkisiz veri okuma  
- Role escalation  
- Field injection  
- UID spoofing  
- Client-side delete  

------------------------------------------------------------

## Özet

Database Security Logic:

- Auth tabanlı **ownership modeli** kullanır  
- **Schema enforcement** uygular  
- **Immutable alanları** korur  
- Client tarafına **minimum yetki** verir  
- Backend logic ile **birebir senkron** çalışır  

> Güvenlik, client’a güvenerek değil,  
> **kurallarla zorlayarak** sağlanır.

============================================================
