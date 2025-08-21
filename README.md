# TemporaryKeyboardSwitcher

---

## 🇬🇧 English

**TemporaryKeyboardSwitcher** — a lightweight tool that allows you to temporarily switch your keyboard to another language without permanently installing an additional Windows keyboard layout.  
Useful when you only occasionally need another language, without cluttering your system with extra layouts.

---

### ✨ Features
- Toggle overlay **on/off** using **Ctrl+Shift+U** (or tray icon).  
- Works in browsers, messengers, and most text input fields.  
- Does **not** require installing additional Windows keyboard layouts.  
- Can be easily customized for any language.  

---

### 🔧 How to change language
If you want to use a different language, replace this part of the code in `Program.cs` with your own keyboard layout mapping:

```csharp
private static readonly Dictionary<Keys, string> MapUaLower = new()
{
    { Keys.Q, "й" }, { Keys.W, "ц" }, { Keys.E, "у" }, { Keys.R, "к" }, { Keys.T, "е" },
    { Keys.Y, "н" }, { Keys.U, "г" }, { Keys.I, "ш" }, { Keys.O, "щ" }, { Keys.P, "з" },
    { Keys.OemOpenBrackets, "х" }, { Keys.OemCloseBrackets, "ї" },
    { Keys.A, "ф" }, { Keys.S, "і" }, { Keys.D, "в" }, { Keys.F, "а" }, { Keys.G, "п" },
    { Keys.H, "р" }, { Keys.J, "о" }, { Keys.K, "л" }, { Keys.L, "д" },
    { Keys.Oem1, "ж" }, { Keys.Oem7, "є" },
    { Keys.Z, "я" }, { Keys.X, "ч" }, { Keys.C, "с" }, { Keys.V, "м" }, { Keys.B, "и" },
    { Keys.N, "т" }, { Keys.M, "ь" }, { Keys.Oemcomma, "б" }, { Keys.OemPeriod, "ю" },
    { Keys.OemQuestion, "." },
    { Keys.Oemtilde, "ґ" },
};
```

Replace the values with your own layout for the language you need.  

---

### ⚙️ Build
1. Open the solution in **Visual Studio**.  
2. Build the project (`Ctrl+Shift+B`).  
3. Run the application — it will appear in the system tray.  

---

---

## 🇺🇦 Українською

**TemporaryKeyboardSwitcher** — це невелика утиліта, яка дозволяє тимчасово перемикати клавіатуру на українські літери (або будь-які інші, які ви самі вкажете), без встановлення додаткової розкладки Windows.  
Зручно, коли українська мова потрібна не постійно, а лише час від часу.

---

### ✨ Можливості
- Увімкнення/вимкнення оверлею комбінацією **Ctrl+Shift+U** (або кліком на іконку в треї).  
- Працює у браузерах, месенджерах та більшості полів введення.  
- **Не потрібно** встановлювати українську розкладку в Windows.  
- Легко змінити під будь-яку іншу мову.  

---

### 🔧 Як змінити мову
Якщо хочете використати іншу мову — замініть цей фрагмент коду в `Program.cs`:

```csharp
private static readonly Dictionary<Keys, string> MapUaLower = new()
{
    { Keys.Q, "й" }, { Keys.W, "ц" }, { Keys.E, "у" }, { Keys.R, "к" }, { Keys.T, "е" },
    { Keys.Y, "н" }, { Keys.U, "г" }, { Keys.I, "ш" }, { Keys.O, "щ" }, { Keys.P, "з" },
    { Keys.OemOpenBrackets, "х" }, { Keys.OemCloseBrackets, "ї" },
    { Keys.A, "ф" }, { Keys.S, "і" }, { Keys.D, "в" }, { Keys.F, "а" }, { Keys.G, "п" },
    { Keys.H, "р" }, { Keys.J, "о" }, { Keys.K, "л" }, { Keys.L, "д" },
    { Keys.Oem1, "ж" }, { Keys.Oem7, "є" },
    { Keys.Z, "я" }, { Keys.X, "ч" }, { Keys.C, "с" }, { Keys.V, "м" }, { Keys.B, "и" },
    { Keys.N, "т" }, { Keys.M, "ь" }, { Keys.Oemcomma, "б" }, { Keys.OemPeriod, "ю" },
    { Keys.OemQuestion, "." },
    { Keys.Oemtilde, "ґ" },
};
```

Підставте свої символи — і утиліта буде вводити їх замість українських.  

---

### ⚙️ Збірка
1. Відкрийте проект у **Visual Studio**.  
2. Зберіть проект (`Ctrl+Shift+B`).  
3. Запустіть програму — іконка з’явиться в системному треї.  
