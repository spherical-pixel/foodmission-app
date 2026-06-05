v0.0.6 - Changelog
✨ New Features
- Meal Log redesigned: 3-step progressive flow (type → source → compose) with separate meal name field and dedicated "Load preset" popover
- Smart save logic: snapshot diff detects modifications — reuses existing meal when unchanged, updates when modified with same name, creates new meal when name changed
- Recipe ingredient dedup: duplicate generic foods in recipe ingredients are now merged (quantities combined)
- FMButton component: new reusable button built on AppUI.UI.Button with shadow support
- Settings screens: un-commented and wired up settings/preferences now that API supports them

🐛 Bug Fixes
- Recipe detection: added isRecipe discriminator to Meal model — recipeId alone is unreliable since meals created from recipes also have it
- Search result labels: FMSearchOrCategoryField now correctly shows GenericFood.foodName instead of C# type name in confirmation panel
- Async void safety: OnPresetSearchChanged wrapped in try-catch to prevent crashes on API failure
- ShoppingListDetail: _progressLabel now properly assigned in CacheUIElements
- Safe area: removed duplicate safearea check from MealLogScreen, restored in PantryScreen.uxml

♻️ Refactors
- FMQuantityUnitPanel: extracted from 5 duplicate qty/unit implementations into reusable component
- Preset search results: now use global fm-scf-result-row class for visual consistency with search results

🧪 Tests
- 8 new tests covering HasModifications, SaveAsync branches, and ConfirmUpdateAndSave

v0.0.5 — Changelog
🆕 Added
- FMSearchOrCategoryField — unified search, category browsing, and barcode scan button for Pantry and ShoppingListDetail screens, replacing FMProductSearchDialog

🎨 Updated
- Avatar editor — cleaned up UXML (removed AppUI theme classes from root, fixed stylesheet references, removed placeholder text)

🔧 Infrastructure
- Model naming migration — food → foodProduct, foodCategory → genericFood to align with API v1
- Localization updates — changed preliminary machine translations to only English by now

v0.0.4 — Changelog
🆕 Added
- Partial screen reader support — AccessibilityService + IAccessibleComponent for 26 screens
- App update check — AppUpdateService fetches version from GitHub, shows ForceUpdateScreen with forced/optional flows
- API environment switching — ApiEnvironmentConfig ScriptableObject to toggle between Staging/Test/Local at runtime
- FMItemListShoppingList — reusable shopping list item component
- FMSearchOrCreateField — reusable search-or-create input component

🎨 Updated
- ShoppingListScreen redesign with new item component and improved layout
- AlertDialog styles and panel adjustments
- Form field components (arrow steppers, checkbox, dropdown, int field, text field) now implement IAccessibleComponent
- Navigation graph updates and cleanup
- FMDialog, LoginScreen, RegisterScreen, SettingsScreen, splash flow, onboarding screens, profile screens, food waste screens, groups screens — accessibility integration and layout fixes
🐛 Fixed

- Arrow stepper flex layout — FMArrowStepper and FormFieldItemArrowStepperSettings now properly shrink and grow
- Avatar editor: Save/Exit button visibility per entry point, AppBar behavior
- FMDialog button layout and icon spacing
- FormFieldItemBase label and input alignment
- Shopping list view model null filter fix
- Disabled Unity Connect service
🔧 Infrastructure

- Updated Foodmission.slnx
- FoodmissionApp.cs and FoodmissionAppBuilder.cs — service registration for Accessibility and AppUpdate
- latest-version.json — version manifest for app update feature

v0.0.3 — Changelog
🆕 Added
- Barcode scanner — preliminary support with camera preview and ZXing decoding (BarcodeScanOverlay)
- Nutri camera — functions to enable/disable cameras in Avatar and Nutri (interactive mascot)
- iOS & Android metadata for localizations (App Store / Play Store descriptions, keywords, etc.)

🐛 Fixed
- Avatar Editor: error preventing all customization options from showing; fixed eyebrows ordering
- Avatar materials: now using MaterialPropertyBlock to avoid dirtying assets on disk
- Android system bars: transparency and re-application after notification shade dismiss
- KeyboardPanelAdjuster: general virtual keyboard panel adjustment
- KeyboardService on Android: proper fallback when TouchScreenKeyboard.area.height returns 0
- Localization: minor preload issue

🎨 Optimized
- RenderTextures: reduced sizes for performance (Avatar + Nutri)

🔧 Other
- Added missing localization assets
- Store upload preparation (version 0.0.3)

v0.0.2 — Changelog
🆕 Added
- Authentication & session: login, register, forgot password, token refresh with proactive timer, Keycloak logout
- Home dashboard: calorie circular progress, status bars, day/meal steppers
- Meal Logging: full CRUD with in-memory cache, meal search, create on-the-fly
- Pantry: items CRUD, food import from barcode, food category service with cache
- Shopping Lists: full CRUD for lists and items, toggle checked, clear checked, barcode import
- Groups: CRUD, join via invite code, member management (add/remove/make-admin)
- Food Waste: logging, statistics, trends
- Avatar Editor: 3D character customization (hair, eyes, nose, mouth, skin, clothes), onboarding integration
- Profile screen: avatar header, stats, action buttons
- Settings screen: font selector, theme (light/dark/system), scale, language
- Notifications: bottom sheet panel with notification cards, overflow menu (view/delete)
- Nutrition mascot (Nutri): 3D model, animations, render texture, dialog screens
- FMDialog: reusable dialog component (alert, confirm, scrollable, custom)
- Form components: text fields, password, dropdown, checkbox, stepper, arrow stepper
- Components: FMStatusBar, AvatarController, FMProductSearchDialog, BarcodeScanOverlay
- Services: TemplateService (UXML from addressables), CatalogService (startup reference data), KeyboardService, NutriService

🔧 Infrastructure
- Unity 6 upgrade (6000.3.12f1), URP, AppUI v2.1.6, Input System
- MVVM + Redux architecture with centralized AppState
- Navigation graph with declarative routing for all screens
- Addressables for UXML/USS asset management
- Localization system connected to Google Sheets, AppUI tables
- TemplateService for loading UXML from addressables with preloading during splash
- Newtonsoft.Json 3.2.1 for OpenFoodFacts deserialization
- All screens migrated from inline loading to TemplateService

🐛 Fixed
- Navigation stopping after login
- Duplicated NutriController on hot reload
- Nutri RenderTexture not working on Android/iOS
- Bottom nav bar label overflow
- Stepers styling
- Token refresh flow
- Missing localizations and preload issues
- Dialog/Modal API usage in FMDialog
- AppUI namespace ambiguities (TextField, Button, Q<>)
- Checkbox value type (bool → CheckboxState)
- FloatField placeholder vs label
- Profile drawer integration with nav framework

🎨 Styling & Theme
- Complete light/dark themes with green-based palette
- Font system: OpenSans, Roboto, OpenDyslexic (Regular/Bold SDF)
- Global keyboard avoidance for mobile (iOS/Android)
- Safe area handling via code
- All USS/TSS organized and cleaned
- Custom arrow icons and app icons

🧪 Testing
- Model serialization tests (Food, ShoppingList, Group, Pantry, Meal, etc.)
- Service tests for Auth, Profile, etc.
- FMDialogAction tests
- Shopping list test fixes
