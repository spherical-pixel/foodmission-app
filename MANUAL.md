# Pasos manuales pendientes en Unity Editor

## 1. Marcar UXML como Addressable (TemplateService)

Para que `TemplateService.PreloadAllAsync()` funcione, cada UXML debe estar marcado como Addressable con la dirección exacta que aparece en `TemplateAddresses.cs`.

**Cómo hacerlo para cada archivo:**
1. Selecciona el archivo `.uxml` en la ventana Project
2. En el Inspector, marca la casilla **Addressable**
3. Establece la dirección al valor de la tabla de abajo (borra el valor por defecto)
4. Repite para los 10 archivos

| Archivo UXML | Dirección Addressable |
|---|---|
| `Assets/Foodmission/scripts/AppUI/Screens/Home/HomeScreen.uxml` | `Foodmission/AppUI/Templates/HomeScreen.uxml` |
| `Assets/Foodmission/scripts/AppUI/Screens/Login/LoginScreen.uxml` | `Foodmission/AppUI/Templates/LoginScreen.uxml` |
| `Assets/Foodmission/scripts/AppUI/Screens/Register/RegisterScreen.uxml` | `Foodmission/AppUI/Templates/RegisterScreen.uxml` |
| `Assets/Foodmission/scripts/AppUI/Screens/ForgotPassword/ForgotPasswordScreen.uxml` | `Foodmission/AppUI/Templates/ForgotPasswordScreen.uxml` |
| `Assets/Foodmission/scripts/AppUI/Screens/Profile/ProfileScreen.uxml` | `Foodmission/AppUI/Templates/ProfileScreen.uxml` |
| `Assets/Foodmission/scripts/AppUI/Screens/Settings/SettingsScreen.uxml` | `Foodmission/AppUI/Templates/SettingsScreen.uxml` |
| `Assets/Foodmission/scripts/AppUI/Screens/Onboarding/OnboardingProfileScreen.uxml` | `Foodmission/AppUI/Templates/OnboardingProfileScreen.uxml` |
| `Assets/Foodmission/scripts/AppUI/Screens/CompleteWelcome/CompleteWelcome.uxml` | `Foodmission/AppUI/Templates/CompleteWelcome.uxml` |
| `Assets/Foodmission/scripts/AppUI/Screens/ShoppingList/ShoppingListScreen.uxml` | `Foodmission/AppUI/Templates/ShoppingListScreen.uxml` |
| `Assets/Foodmission/scripts/AppUI/Screens/ShoppingList/ShoppingListDetailScreen.uxml` | `Foodmission/AppUI/Templates/ShoppingListDetailScreen.uxml` |

**No marcar como Addressable:**
- `SplashScreen.uxml` — se carga desde Inspector, no desde TemplateService
- `NotificationCard.uxml` — ídem

**Verificar después:** En Play Mode, la splash screen debe progresar sin errores `[TemplateService] Failed to load template` en la consola.

---

## 2. Asignar templates de Shopping List en el Inspector

Las dos pantallas de Shopping List tienen templates UXML nuevos que deben asignarse al componente `FoodmissionAppBuilder` en la escena.

1. Abre la escena principal: `Assets/Foodmission/scenes/FoodmissionAppUI.unity`
2. Selecciona el GameObject `FoodmissionAppBuilder` en la jerarquía
3. En el Inspector, asigna:
   - Campo **Shopping List Template** → `ShoppingListScreen.uxml`
   - Campo **Shopping List Detail Template** → `ShoppingListDetailScreen.uxml`

> Nota: una vez completado el paso 1 (Addressables), estos campos dejarán de ser necesarios porque los templates se cargarán por dirección. Pero hasta que el paso 1 esté hecho, sin esta asignación las pantallas aparecerán en blanco.

---

## 3. Commitear cambios de .meta tras marcar Addressables

Después de marcar los 10 UXML como Addressable, Unity modifica sus archivos `.meta`. Hacer commit de esos cambios:

```bash
git add Assets/Foodmission/scripts/AppUI/Screens/Home/HomeScreen.uxml.meta
git add Assets/Foodmission/scripts/AppUI/Screens/Login/LoginScreen.uxml.meta
git add Assets/Foodmission/scripts/AppUI/Screens/Register/RegisterScreen.uxml.meta
git add Assets/Foodmission/scripts/AppUI/Screens/ForgotPassword/ForgotPasswordScreen.uxml.meta
git add Assets/Foodmission/scripts/AppUI/Screens/Profile/ProfileScreen.uxml.meta
git add Assets/Foodmission/scripts/AppUI/Screens/Settings/SettingsScreen.uxml.meta
git add Assets/Foodmission/scripts/AppUI/Screens/Onboarding/OnboardingProfileScreen.uxml.meta
git add Assets/Foodmission/scripts/AppUI/Screens/CompleteWelcome/CompleteWelcome.uxml.meta
git add Assets/Foodmission/scripts/AppUI/Screens/ShoppingList/ShoppingListScreen.uxml.meta
git add Assets/Foodmission/scripts/AppUI/Screens/ShoppingList/ShoppingListDetailScreen.uxml.meta
git commit -m "chore: mark screen UXML templates as Addressable assets"
```
