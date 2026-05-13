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
