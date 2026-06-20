const { StyleSheet } = require("react-native");
const { defaultThemeKey, themeMap } = require("../core/app-config");
const { normalizeThemeKey } = require("../core/formatters");

function createPalette(themeKey) {
  return themeMap.get(normalizeThemeKey(themeKey)).colors;
}

function createStyles(palette) {
  return StyleSheet.create({
  app: {
    flex: 1,
    backgroundColor: palette.bg
  },
  appFrame: {
    flex: 1,
    minHeight: 0,
    flexDirection: "row",
    backgroundColor: palette.bg
  },
  navBackdrop: {
    position: "absolute",
    top: 0,
    right: 0,
    bottom: 0,
    left: 0,
    zIndex: 10,
    backgroundColor: "rgba(0,0,0,0.48)"
  },
  contentPane: {
    flex: 1,
    minWidth: 0,
    minHeight: 0,
    flexDirection: "column",
    overflow: "hidden",
    backgroundColor: palette.bg
  },
  contentBody: {
    flex: 1,
    minHeight: 0,
    overflow: "hidden"
  },
  smartSearchShell: {
    zIndex: 25,
    flexShrink: 0,
    paddingHorizontal: 14,
    paddingTop: 12,
    paddingBottom: 8,
    backgroundColor: palette.bg
  },
  smartSearchInputRow: {
    minHeight: 54,
    flexDirection: "row",
    alignItems: "center",
    gap: 10,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.accent,
    backgroundColor: palette.surface,
    paddingHorizontal: 12,
    shadowColor: palette.accent,
    shadowOpacity: 0.18,
    shadowRadius: 18,
    shadowOffset: { width: 0, height: 6 },
    elevation: 10
  },
  smartSearchMark: {
    width: 32,
    height: 32,
    borderRadius: 2,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: palette.accent
  },
  smartSearchMarkText: {
    color: "#ffffff",
    fontWeight: "900"
  },
  smartSearchInput: {
    flex: 1,
    minWidth: 0,
    minHeight: 42,
    color: palette.text,
    fontSize: 14,
    paddingVertical: 0
  },
  smartSearchStatus: {
    maxWidth: 120,
    color: palette.muted,
    fontSize: 11,
    fontWeight: "800",
    textAlign: "right"
  },
  smartSearchPanel: {
    maxHeight: 360,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    marginTop: 8,
    padding: 10,
    gap: 9
  },
  smartSearchEmpty: {
    gap: 4
  },
  smartSearchEmptyTitle: {
    color: palette.text,
    fontSize: 13,
    fontWeight: "900"
  },
  smartSearchEmptyText: {
    color: palette.muted,
    fontSize: 12,
    lineHeight: 17
  },
  smartSearchChipRail: {
    gap: 8
  },
  smartSearchChip: {
    minHeight: 34,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    justifyContent: "center",
    paddingHorizontal: 12
  },
  smartSearchChipText: {
    color: palette.text,
    fontWeight: "800"
  },
  smartSearchResultsScroll: {
    maxHeight: 260
  },
  smartSearchResultsContent: {
    gap: 9,
    paddingBottom: 2
  },
  smartSearchGroup: {
    gap: 6
  },
  smartSearchGroupTitle: {
    color: palette.soft,
    fontSize: 10,
    fontWeight: "900",
    textTransform: "uppercase"
  },
  smartSearchResult: {
    minHeight: 54,
    borderRadius: 2,
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 10,
    backgroundColor: palette.surface2,
    paddingHorizontal: 10,
    paddingVertical: 8
  },
  smartSearchResultCopy: {
    flex: 1,
    minWidth: 0
  },
  smartSearchResultTitle: {
    color: palette.text,
    fontWeight: "900"
  },
  smartSearchResultSubtitle: {
    color: palette.muted,
    fontSize: 12,
    marginTop: 3
  },
  smartSearchResultBadge: {
    maxWidth: 116,
    color: palette.accent,
    fontSize: 11,
    fontWeight: "900",
    textAlign: "right"
  },
  topBar: {
    minHeight: 64,
    flexShrink: 0,
    flexDirection: "row",
    alignItems: "center",
    gap: 12,
    paddingHorizontal: 16,
    paddingVertical: 10,
    borderBottomWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.bg,
    shadowColor: "#000000",
    shadowOpacity: 0.12,
    shadowRadius: 8,
    shadowOffset: { width: 0, height: 2 },
    elevation: 4
  },
  menuButton: {
    minWidth: 68,
    minHeight: 40,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    alignItems: "center",
    justifyContent: "center",
    paddingHorizontal: 12
  },
  menuButtonText: {
    color: palette.text,
    fontWeight: "900",
    fontSize: 12
  },
  topBarCopy: {
    flex: 1,
    minWidth: 0
  },
  topBarTitle: {
    color: palette.text,
    fontSize: 18,
    fontWeight: "900"
  },
  topBarSubtitle: {
    color: palette.muted,
    fontSize: 11,
    marginTop: 2
  },
  topBarUser: {
    maxWidth: 124,
    alignItems: "flex-end"
  },
  topBarUserName: {
    color: palette.text,
    fontSize: 12,
    fontWeight: "800"
  },
  topBarUserRole: {
    color: palette.muted,
    fontSize: 10,
    marginTop: 2
  },
  sidePanel: {
    width: 292,
    flexShrink: 0,
    zIndex: 30,
    borderRightWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.sidebar
  },
  sidePanelOverlay: {
    position: "absolute",
    top: 0,
    bottom: 0,
    left: 0,
    width: 304,
    elevation: 12
  },
  sideHeader: {
    minHeight: 72,
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 10,
    paddingHorizontal: 14,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderColor: palette.line
  },
  sideBrandRow: {
    flex: 1,
    flexDirection: "row",
    alignItems: "center",
    gap: 10,
    minWidth: 0
  },
  sideBrandMark: {
    width: 38,
    height: 38,
    borderRadius: 2,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: palette.accent
  },
  sideBrandMarkText: {
    color: palette.text,
    fontSize: 20,
    fontWeight: "900"
  },
  sideBrandCopy: {
    minWidth: 0
  },
  sideBrandTitle: {
    color: palette.text,
    fontSize: 16,
    fontWeight: "900"
  },
  sideBrandSubtitle: {
    color: palette.muted,
    fontSize: 11,
    marginTop: 2
  },
  sideCloseButton: {
    minHeight: 34,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    alignItems: "center",
    justifyContent: "center",
    paddingHorizontal: 10
  },
  sideCloseText: {
    color: palette.text,
    fontSize: 11,
    fontWeight: "900"
  },
  sideNav: {
    flex: 1
  },
  sideNavContent: {
    padding: 12,
    gap: 16,
    paddingBottom: 18
  },
  navGroup: {
    gap: 7
  },
  navGroupTitle: {
    color: palette.soft,
    fontSize: 11,
    fontWeight: "900",
    textTransform: "uppercase"
  },
  sideNavItem: {
    minHeight: 42,
    flexDirection: "row",
    alignItems: "center",
    gap: 10,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "transparent",
    backgroundColor: "transparent",
    paddingHorizontal: 10
  },
  sideNavItemActive: {
    borderColor: palette.line,
    backgroundColor: palette.surface2
  },
  navActiveMark: {
    width: 4,
    height: 22,
    borderRadius: 2,
    backgroundColor: "transparent"
  },
  navActiveMarkOn: {
    backgroundColor: palette.accent
  },
  sideNavText: {
    flex: 1,
    color: palette.muted,
    fontSize: 13,
    fontWeight: "800"
  },
  sideNavTextActive: {
    color: palette.text
  },
  sideThemeBlock: {
    gap: 9,
    paddingTop: 2
  },
  sideThemeGrid: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 8
  },
  sideThemeButton: {
    width: "48%",
    minHeight: 34,
    flexDirection: "row",
    alignItems: "center",
    gap: 7,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: "transparent",
    paddingHorizontal: 8
  },
  sideThemeButtonActive: {
    borderColor: palette.accent,
    backgroundColor: palette.surface2
  },
  sideThemeText: {
    flex: 1,
    color: palette.muted,
    fontSize: 11,
    fontWeight: "800"
  },
  sideThemeTextActive: {
    color: palette.text
  },
  settingsLanguageButton: {
    width: "48%",
    minHeight: 42,
    flexDirection: "row",
    alignItems: "center",
    gap: 8,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    paddingHorizontal: 9
  },
  settingsLanguageButtonActive: {
    borderColor: palette.accent,
    backgroundColor: palette.input
  },
  settingsLanguageMark: {
    width: 34,
    height: 28,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface
  },
  settingsLanguageMarkActive: {
    borderColor: palette.accent,
    backgroundColor: palette.accent
  },
  settingsLanguageMarkText: {
    color: palette.muted,
    fontSize: 11,
    fontWeight: "900"
  },
  settingsLanguageMarkTextActive: {
    color: palette.text
  },
  sideUserPanel: {
    borderTopWidth: 1,
    borderColor: palette.line,
    padding: 12,
    gap: 10
  },
  sideAvatar: {
    width: 38,
    height: 38,
    borderRadius: 19,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: palette.whatsapp
  },
  sideAvatarText: {
    color: palette.text,
    fontWeight: "900"
  },
  sideUserCopy: {
    minWidth: 0
  },
  sideUserName: {
    color: palette.text,
    fontSize: 13,
    fontWeight: "900"
  },
  sideUserRole: {
    color: palette.muted,
    fontSize: 11,
    marginTop: 2
  },
  sideLogoutButton: {
    minHeight: 38,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2
  },
  sideLogoutText: {
    color: palette.text,
    fontSize: 12,
    fontWeight: "900"
  },
  bootScreen: {
    flex: 1,
    alignItems: "center",
    justifyContent: "center",
    gap: 14,
    backgroundColor: palette.bg
  },
  mutedText: {
    color: palette.muted
  },
  loginScreen: {
    flex: 1,
    backgroundColor: "#04050a"
  },
  loginBackdrop: {
    flex: 1,
    backgroundColor: "#04050a"
  },
  loginBackdropImage: {
    opacity: 0.98
  },
  loginShade: {
    flex: 1,
    backgroundColor: "rgba(4,5,10,0.10)"
  },
  loginWarmGlow: {
    position: "absolute",
    top: -60,
    right: 28,
    width: 32,
    height: 460,
    borderRadius: 2,
    backgroundColor: "rgba(255,59,31,0.20)",
    transform: [{ rotate: "14deg" }]
  },
  loginCoolGlow: {
    position: "absolute",
    top: 10,
    left: -28,
    width: 24,
    height: 340,
    borderRadius: 2,
    backgroundColor: "rgba(70,170,210,0.13)",
    transform: [{ rotate: "-18deg" }]
  },
  loginRoadFade: {
    position: "absolute",
    left: 0,
    right: 0,
    bottom: 0,
    height: "50%",
    backgroundColor: "rgba(5,6,9,0.42)"
  },
  loginKeyboard: {
    flex: 1
  },
  loginScroll: {
    flex: 1
  },
  loginScrollContent: {
    flexGrow: 1,
    justifyContent: "space-between",
    gap: 18,
    paddingHorizontal: 18,
    paddingTop: 24,
    paddingBottom: 18
  },
  loginScrollContentKeyboard: {
    justifyContent: "flex-end",
    gap: 10,
    paddingTop: 10,
    paddingBottom: 12
  },
  loginPoster: {
    minHeight: 238,
    justifyContent: "flex-end",
    gap: 16
  },
  loginFuelStrip: {
    width: 132,
    height: 5,
    flexDirection: "row",
    overflow: "hidden",
    borderRadius: 2,
    backgroundColor: "rgba(255,255,255,0.18)"
  },
  loginFuelStripHot: {
    flex: 3,
    backgroundColor: "#e85012"
  },
  loginFuelStripWarm: {
    flex: 2,
    backgroundColor: "#f59e0b"
  },
  loginFuelStripCool: {
    flex: 1,
    backgroundColor: "#f4f5f7"
  },
  loginBrandRow: {
    flexDirection: "row",
    alignItems: "center",
    gap: 14
  },
  loginBrandCopy: {
    flex: 1,
    minWidth: 0
  },
  loginPanel: {
    padding: 20,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(255,255,255,0.16)",
    borderTopColor: "rgba(255,255,255,0.22)",
    backgroundColor: "rgba(7,9,14,0.90)",
    gap: 14,
    overflow: "hidden",
    shadowColor: "#000000",
    shadowOpacity: 0.4,
    shadowRadius: 28,
    shadowOffset: { width: 0, height: 12 }
  },
  loginPanelKeyboard: {
    padding: 14,
    gap: 10
  },
  loginBrand: {
    alignItems: "center",
    gap: 8,
    marginBottom: 8
  },
  loginPanelHeader: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 12,
    paddingBottom: 2
  },
  loginPanelKicker: {
    color: "#f59e0b",
    fontSize: 11,
    fontWeight: "900",
    textTransform: "uppercase"
  },
  loginPanelTitle: {
    color: "#f8fafc",
    fontSize: 22,
    fontWeight: "900",
    marginTop: 2
  },
  loginPanelBadge: {
    minWidth: 46,
    minHeight: 34,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(245,158,11,0.44)",
    backgroundColor: "rgba(245,158,11,0.13)"
  },
  loginPanelBadgeText: {
    color: "#fbbf24",
    fontSize: 11,
    fontWeight: "900"
  },
  loginEyebrow: {
    color: "#f59e0b",
    fontSize: 11,
    fontWeight: "900",
    textTransform: "uppercase"
  },
  loginTitle: {
    color: "#ffffff",
    fontSize: 42,
    lineHeight: 44,
    fontWeight: "900"
  },
  loginSubtitle: {
    color: "#d8dbe2",
    fontSize: 14,
    lineHeight: 20
  },
  loginTelemetry: {
    flexDirection: "row",
    gap: 8
  },
  loginTelemetryCell: {
    flex: 1,
    minHeight: 54,
    justifyContent: "center",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(255,255,255,0.16)",
    backgroundColor: "rgba(6,7,10,0.54)",
    paddingHorizontal: 10
  },
  loginTelemetryLabel: {
    color: "#a9afbd",
    fontSize: 10,
    fontWeight: "900"
  },
  loginTelemetryValue: {
    color: "#ffffff",
    fontSize: 15,
    fontWeight: "900",
    marginTop: 3
  },
  socialLogin: {
    gap: 10,
    paddingTop: 2
  },
  socialDivider: {
    flexDirection: "row",
    alignItems: "center",
    gap: 10
  },
  socialDividerLine: {
    flex: 1,
    height: 1,
    backgroundColor: "rgba(255,255,255,0.16)"
  },
  socialDividerText: {
    color: "#a9afbd",
    fontSize: 11,
    fontWeight: "900",
    textTransform: "uppercase"
  },
  socialButton: {
    minHeight: 46,
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "center",
    gap: 10,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(255,255,255,0.18)",
    backgroundColor: "rgba(255,255,255,0.08)",
    paddingHorizontal: 14
  },
  facebookButton: {
    backgroundColor: "#1877f2",
    borderColor: "#1877f2"
  },
  socialBrandMark: {
    width: 24,
    height: 24,
    borderRadius: 2,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: "#fff"
  },
  socialBrandText: {
    color: "#202124",
    fontSize: 14,
    fontWeight: "900"
  },
  facebookBrandMark: {
    width: 24,
    height: 24,
    borderRadius: 2,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: "#fff"
  },
  facebookBrandText: {
    color: "#1877f2",
    fontSize: 18,
    fontWeight: "900"
  },
  socialButtonText: {
    color: "#ffffff",
    fontSize: 13,
    fontWeight: "900"
  },
  brandMarkLarge: {
    width: 62,
    height: 62,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(255,255,255,0.30)",
    backgroundColor: "#e85012",
    alignItems: "center",
    justifyContent: "center"
  },
  brandMarkLargeText: {
    color: "#ffffff",
    fontSize: 32,
    fontWeight: "900"
  },
  header: {
    flexDirection: "row",
    alignItems: "center",
    gap: 12,
    paddingHorizontal: 16,
    paddingTop: 12,
    paddingBottom: 10,
    borderBottomWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.sidebar
  },
  brandMark: {
    width: 38,
    height: 38,
    borderRadius: 2,
    backgroundColor: palette.accent,
    alignItems: "center",
    justifyContent: "center"
  },
  brandMarkText: {
    color: palette.text,
    fontWeight: "900",
    fontSize: 20
  },
  headerCopy: {
    flex: 1
  },
  headerTitle: {
    color: palette.text,
    fontWeight: "800",
    fontSize: 16
  },
  headerSubtitle: {
    color: palette.muted,
    fontSize: 12,
    marginTop: 2
  },
  headerButton: {
    paddingHorizontal: 10,
    paddingVertical: 8,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2
  },
  headerButtonText: {
    color: palette.text,
    fontSize: 12,
    fontWeight: "700"
  },
  tabBar: {
    backgroundColor: palette.sidebar,
    borderBottomWidth: 1,
    borderColor: palette.line
  },
  tabBarContent: {
    flexDirection: "row",
    gap: 8,
    paddingHorizontal: 12,
    paddingVertical: 10
  },
  tabButton: {
    minWidth: 96,
    paddingVertical: 9,
    paddingHorizontal: 12,
    borderRadius: 2,
    alignItems: "center",
    backgroundColor: "transparent",
    borderWidth: 1,
    borderColor: "transparent"
  },
  tabButtonActive: {
    backgroundColor: palette.surface2,
    borderColor: palette.line
  },
  tabButtonText: {
    color: palette.muted,
    fontSize: 11,
    fontWeight: "700"
  },
  tabButtonTextActive: {
    color: palette.text
  },
  bottomTabBar: {
    height: 86,
    flexShrink: 0,
    flexDirection: "row",
    alignItems: "stretch",
    justifyContent: "space-between",
    gap: 0,
    paddingHorizontal: 4,
    paddingTop: 4,
    paddingBottom: 8,
    borderTopWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.sidebar,
    elevation: 24,
    shadowColor: "#000000",
    shadowOpacity: 0.38,
    shadowRadius: 18,
    shadowOffset: { width: 0, height: -6 }
  },
  bottomTabButton: {
    flex: 1,
    minWidth: 0,
    alignItems: "center",
    justifyContent: "center",
    gap: 3,
    borderRadius: 2,
    backgroundColor: "transparent",
    paddingHorizontal: 6,
    paddingVertical: 8,
    marginHorizontal: 2
  },
  bottomTabButtonActive: {
    backgroundColor: palette.surface2
  },
  bottomTabMark: {
    width: 48,
    height: 34,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: "transparent",
    borderRadius: 2
  },
  bottomTabMarkActive: {
    backgroundColor: "transparent"
  },
  bottomTabMarkText: {
    color: palette.muted,
    fontSize: 9,
    fontWeight: "900"
  },
  bottomTabMarkTextActive: {
    color: palette.accent
  },
  bottomTabLabel: {
    maxWidth: "100%",
    color: palette.muted,
    fontSize: 10,
    fontWeight: "800",
    textAlign: "center",
    letterSpacing: 0.2
  },
  bottomTabLabelActive: {
    color: palette.accent,
    fontWeight: "900"
  },
  bottomTabDescription: {
    maxWidth: "100%",
    color: palette.soft,
    fontSize: 8,
    fontWeight: "800",
    textAlign: "center"
  },
  bottomTabDescriptionActive: {
    color: palette.muted
  },
  bottomIconBox: {
    width: 24,
    height: 18,
    position: "relative"
  },
  bottomIconLine: {
    borderColor: palette.muted,
    backgroundColor: palette.muted
  },
  bottomIconLineActive: {
    borderColor: palette.text,
    backgroundColor: palette.text
  },
  bottomIconFill: {
    backgroundColor: palette.muted,
    borderColor: palette.muted
  },
  bottomIconFillActive: {
    backgroundColor: palette.text,
    borderColor: palette.text
  },
  iconGaugeArc: {
    position: "absolute",
    left: 3,
    top: 4,
    width: 18,
    height: 12,
    borderTopWidth: 2,
    borderLeftWidth: 2,
    borderRightWidth: 2,
    borderTopLeftRadius: 12,
    borderTopRightRadius: 12,
    backgroundColor: "transparent"
  },
  iconGaugeNeedle: {
    position: "absolute",
    left: 12,
    top: 8,
    width: 2,
    height: 8,
    borderRadius: 2,
    transform: [{ rotate: "42deg" }]
  },
  iconGaugeHub: {
    position: "absolute",
    left: 10,
    top: 13,
    width: 5,
    height: 5,
    borderRadius: 5
  },
  iconPistonHead: {
    position: "absolute",
    left: 7,
    top: 2,
    width: 12,
    height: 7,
    borderRadius: 2
  },
  iconPistonRod: {
    position: "absolute",
    left: 11,
    top: 8,
    width: 3,
    height: 10,
    borderRadius: 2,
    transform: [{ rotate: "-22deg" }]
  },
  iconPistonPin: {
    position: "absolute",
    left: 8,
    top: 14,
    width: 12,
    height: 2,
    borderRadius: 2,
    transform: [{ rotate: "-22deg" }]
  },
  iconCartBasket: {
    position: "absolute",
    left: 5,
    top: 5,
    width: 15,
    height: 8,
    borderWidth: 2,
    borderTopWidth: 0,
    backgroundColor: "transparent",
    transform: [{ skewX: "-10deg" }]
  },
  iconCartHandle: {
    position: "absolute",
    left: 2,
    top: 3,
    width: 8,
    height: 2,
    borderRadius: 2,
    transform: [{ rotate: "20deg" }]
  },
  iconCartWheelLeft: {
    position: "absolute",
    left: 7,
    top: 14,
    width: 4,
    height: 4,
    borderRadius: 4
  },
  iconCartWheelRight: {
    position: "absolute",
    left: 17,
    top: 14,
    width: 4,
    height: 4,
    borderRadius: 4
  },
  iconFlagPole: {
    position: "absolute",
    left: 6,
    top: 2,
    width: 2,
    height: 15,
    borderRadius: 2
  },
  iconFlagCloth: {
    position: "absolute",
    left: 8,
    top: 2,
    width: 13,
    height: 8,
    borderTopRightRadius: 3,
    borderBottomRightRadius: 3,
    transform: [{ skewX: "-10deg" }]
  },
  iconKeyRing: {
    position: "absolute",
    left: 2,
    top: 5,
    width: 9,
    height: 9,
    borderRadius: 9,
    borderWidth: 2,
    backgroundColor: "transparent"
  },
  iconKeyShaft: {
    position: "absolute",
    left: 10,
    top: 9,
    width: 12,
    height: 2,
    borderRadius: 2
  },
  iconKeyToothOne: {
    position: "absolute",
    left: 18,
    top: 10,
    width: 2,
    height: 5,
    borderRadius: 2
  },
  iconKeyToothTwo: {
    position: "absolute",
    left: 21,
    top: 7,
    width: 2,
    height: 5,
    borderRadius: 2
  },
  iconReceiptPage: {
    position: "absolute",
    left: 6,
    top: 1,
    width: 13,
    height: 16,
    borderWidth: 2,
    borderRadius: 2,
    backgroundColor: "transparent"
  },
  iconReceiptLineOne: {
    position: "absolute",
    left: 9,
    top: 6,
    width: 7,
    height: 2,
    borderRadius: 2
  },
  iconReceiptLineTwo: {
    position: "absolute",
    left: 9,
    top: 11,
    width: 6,
    height: 2,
    borderRadius: 2
  },
  iconSparkBody: {
    position: "absolute",
    left: 9,
    top: 4,
    width: 7,
    height: 10,
    borderWidth: 2,
    borderRadius: 2,
    backgroundColor: "transparent",
    transform: [{ rotate: "-18deg" }]
  },
  iconSparkCap: {
    position: "absolute",
    left: 10,
    top: 1,
    width: 8,
    height: 3,
    borderRadius: 2,
    transform: [{ rotate: "-18deg" }]
  },
  iconSparkTip: {
    position: "absolute",
    left: 7,
    top: 13,
    width: 10,
    height: 2,
    borderRadius: 2,
    transform: [{ rotate: "-18deg" }]
  },
  iconWrenchJaw: {
    position: "absolute",
    left: 3,
    top: 2,
    width: 8,
    height: 8,
    borderTopWidth: 2,
    borderLeftWidth: 2,
    borderRadius: 8,
    backgroundColor: "transparent",
    transform: [{ rotate: "-35deg" }]
  },
  iconWrenchHandle: {
    position: "absolute",
    left: 10,
    top: 8,
    width: 13,
    height: 3,
    borderRadius: 2,
    transform: [{ rotate: "-35deg" }]
  },
  iconWrenchGrip: {
    position: "absolute",
    left: 17,
    top: 13,
    width: 6,
    height: 4,
    borderRadius: 2,
    transform: [{ rotate: "-35deg" }]
  },
  themeRail: {
    backgroundColor: palette.sidebar
  },
  themeRailContent: {
    flexDirection: "row",
    gap: 8,
    paddingHorizontal: 12,
    paddingBottom: 8
  },
  themeButton: {
    flexDirection: "row",
    alignItems: "center",
    gap: 7,
    minHeight: 34,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: "transparent",
    paddingHorizontal: 10
  },
  themeButtonActive: {
    backgroundColor: palette.surface2,
    borderColor: palette.accent
  },
  themeDot: {
    width: 13,
    height: 13,
    borderRadius: 2,
    borderWidth: 1
  },
  themeButtonText: {
    color: palette.muted,
    fontSize: 11,
    fontWeight: "800"
  },
  themeButtonTextActive: {
    color: palette.text
  },
  screen: {
    flex: 1,
    minHeight: 0
  },
  screenContent: {
    padding: 16,
    gap: 14,
    paddingBottom: 32
  },
  flexScreen: {
    flex: 1,
    padding: 16,
    gap: 12
  },
  screenHeader: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 12
  },
  screenTitle: {
    color: palette.text,
    fontSize: 26,
    fontWeight: "900",
    letterSpacing: -0.6
  },
  headerAction: {
    minWidth: 86,
    minHeight: 38,
    alignItems: "center",
    justifyContent: "center",
    paddingHorizontal: 12,
    borderRadius: 2,
    backgroundColor: palette.surface2,
    borderWidth: 1,
    borderColor: palette.line
  },
  headerActionText: {
    color: palette.text,
    fontWeight: "800"
  },
  field: {
    gap: 6
  },
  fieldLabel: {
    color: palette.muted,
    fontSize: 12,
    fontWeight: "800"
  },
  input: {
    minHeight: 46,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input,
    color: palette.text,
    paddingHorizontal: 14,
    fontSize: 14
  },
  statusText: {
    color: palette.muted,
    fontSize: 12
  },
  primaryButton: {
    minHeight: 48,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 2,
    backgroundColor: palette.accent,
    paddingHorizontal: 18,
    shadowColor: palette.accent,
    shadowOpacity: 0.34,
    shadowRadius: 12,
    shadowOffset: { width: 0, height: 4 },
    elevation: 6
  },
  compactButton: {
    minHeight: 46
  },
  primaryButtonText: {
    color: "#04080e",
    fontWeight: "900",
    fontSize: 14,
    letterSpacing: 0.2
  },
  secondaryButton: {
    minHeight: 40,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    paddingHorizontal: 14
  },
  secondaryButtonText: {
    color: palette.text,
    fontWeight: "800",
    fontSize: 13
  },
  disabledButton: {
    opacity: 0.55
  },
  partsMetricGrid: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 9
  },
  partsMetric: {
    width: "48%",
    minHeight: 68,
    justifyContent: "space-between",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    padding: 10
  },
  partsMetricLabel: {
    color: palette.muted,
    fontSize: 11,
    fontWeight: "900",
    textTransform: "uppercase"
  },
  partsMetricValue: {
    color: palette.text,
    fontSize: 20,
    fontWeight: "900"
  },
  partsFocusList: {
    gap: 8
  },
  partsFocusRow: {
    minHeight: 54,
    flexDirection: "row",
    alignItems: "center",
    gap: 10,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    padding: 9
  },
  partsFocusRank: {
    width: 30,
    height: 30,
    borderRadius: 2,
    overflow: "hidden",
    color: palette.accent,
    backgroundColor: palette.input,
    textAlign: "center",
    textAlignVertical: "center",
    fontSize: 11,
    fontWeight: "900"
  },
  partsFocusCopy: {
    flex: 1,
    minWidth: 0
  },
  partsFocusTitle: {
    color: palette.text,
    fontSize: 13,
    fontWeight: "900"
  },
  partsFocusSubtitle: {
    color: palette.muted,
    fontSize: 11,
    marginTop: 2
  },
  partsFocusAction: {
    maxWidth: 96,
    color: palette.accent,
    fontSize: 11,
    fontWeight: "900",
    textTransform: "uppercase",
    textAlign: "right"
  },
  partsChipRail: {
    gap: 8,
    paddingRight: 12
  },
  partsChip: {
    minHeight: 36,
    justifyContent: "center",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    paddingHorizontal: 12
  },
  partsChipActive: {
    borderColor: palette.accent,
    backgroundColor: palette.input
  },
  partsChipText: {
    color: palette.muted,
    fontSize: 12,
    fontWeight: "900"
  },
  partsChipTextActive: {
    color: palette.text
  },
  partsPriceRow: {
    flexDirection: "row",
    gap: 10
  },
  partsPriceField: {
    flex: 1,
    minWidth: 0
  },
  partCard: {
    flexDirection: "row",
    gap: 10,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    padding: 10
  },
  partPhoto: {
    width: 74,
    minHeight: 106,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(255,255,255,0.10)",
    backgroundColor: palette.input,
    alignItems: "center",
    justifyContent: "center",
    gap: 7
  },
  partPhotoText: {
    color: palette.accent,
    fontSize: 22,
    fontWeight: "900"
  },
  partPhotoBadge: {
    color: palette.muted,
    fontSize: 9,
    fontWeight: "900"
  },
  partCardBody: {
    flex: 1,
    minWidth: 0,
    gap: 8
  },
  partCardTopline: {
    flexDirection: "row",
    alignItems: "flex-start",
    justifyContent: "space-between",
    gap: 8
  },
  partCardCopy: {
    flex: 1,
    minWidth: 0
  },
  partCode: {
    color: palette.accent,
    fontSize: 10,
    fontWeight: "900",
    textTransform: "uppercase"
  },
  partTitle: {
    color: palette.text,
    fontSize: 15,
    lineHeight: 19,
    fontWeight: "900",
    marginTop: 2
  },
  partPrice: {
    maxWidth: 112,
    color: palette.text,
    fontSize: 13,
    fontWeight: "900",
    textAlign: "right"
  },
  partBadgeRow: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 6
  },
  partsStatus: {
    borderRadius: 2,
    overflow: "hidden",
    paddingHorizontal: 8,
    paddingVertical: 4,
    color: palette.text,
    fontSize: 10,
    fontWeight: "900"
  },
  partsStatusAvailable: {
    backgroundColor: "rgba(37,211,102,0.20)"
  },
  partsStatusReserved: {
    backgroundColor: "rgba(245,158,11,0.20)"
  },
  partsStatusSold: {
    backgroundColor: "rgba(255,107,95,0.18)"
  },
  partsPriority: {
    borderRadius: 2,
    overflow: "hidden",
    paddingHorizontal: 8,
    paddingVertical: 4,
    color: palette.text,
    fontSize: 10,
    fontWeight: "900"
  },
  partsPrioritySuccess: {
    backgroundColor: "rgba(37,211,102,0.18)"
  },
  partsPriorityWarning: {
    backgroundColor: "rgba(245,158,11,0.20)"
  },
  partsPriorityDanger: {
    backgroundColor: "rgba(255,107,95,0.18)"
  },
  partsPriorityNeutral: {
    backgroundColor: palette.input
  },
  partsDemandBadge: {
    borderRadius: 2,
    overflow: "hidden",
    paddingHorizontal: 8,
    paddingVertical: 4,
    color: palette.text,
    fontSize: 10,
    fontWeight: "900",
    backgroundColor: "rgba(214,169,79,0.18)"
  },
  partsCondition: {
    borderRadius: 2,
    overflow: "hidden",
    paddingHorizontal: 8,
    paddingVertical: 4,
    color: palette.muted,
    fontSize: 10,
    fontWeight: "900",
    backgroundColor: "rgba(255,255,255,0.07)"
  },
  partsDonorBadge: {
    borderRadius: 2,
    overflow: "hidden",
    paddingHorizontal: 8,
    paddingVertical: 4,
    color: palette.accent,
    fontSize: 10,
    fontWeight: "900",
    backgroundColor: "rgba(232,80,18,0.14)"
  },
  partMeta: {
    color: palette.muted,
    fontSize: 12,
    lineHeight: 17
  },
  partActionRow: {
    flexDirection: "row",
    gap: 8
  },
  metricGrid: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 10
  },
  metricTile: {
    width: "48%",
    minHeight: 92,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    padding: 14,
    justifyContent: "space-between",
    shadowColor: "#000000",
    shadowOpacity: 0.18,
    shadowRadius: 10,
    shadowOffset: { width: 0, height: 4 },
    elevation: 4
  },
  metricLabel: {
    color: palette.soft,
    fontSize: 10,
    fontWeight: "900",
    textTransform: "uppercase",
    letterSpacing: 0.6
  },
  metricValue: {
    color: palette.text,
    fontSize: 22,
    fontWeight: "900",
    letterSpacing: -0.5,
    marginTop: 4
  },
  metricAction: {
    color: palette.accent,
    fontSize: 10,
    fontWeight: "900",
    marginTop: 6,
    textTransform: "uppercase",
    letterSpacing: 0.4
  },
  dashboardActionGrid: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 10
  },
  dashboardActionButton: {
    width: "48%",
    minHeight: 120,
    justifyContent: "space-between",
    gap: 7,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    padding: 14,
    shadowColor: "#000000",
    shadowOpacity: 0.14,
    shadowRadius: 10,
    shadowOffset: { width: 0, height: 4 },
    elevation: 3
  },
  dashboardActionButtonFeatured: {
    borderColor: palette.accent,
    backgroundColor: palette.surface,
    shadowColor: palette.accent,
    shadowOpacity: 0.18,
    shadowRadius: 14,
    elevation: 5
  },
  dashboardActionBadge: {
    alignSelf: "flex-start",
    color: palette.accent,
    fontSize: 10,
    fontWeight: "900",
    textTransform: "uppercase",
    letterSpacing: 0.6
  },
  dashboardActionTitle: {
    color: palette.text,
    fontSize: 16,
    fontWeight: "900",
    letterSpacing: -0.2
  },
  dashboardActionSubtitle: {
    color: palette.muted,
    fontSize: 12,
    lineHeight: 18
  },
  panel: {
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    padding: 16,
    gap: 12,
    shadowColor: "#000000",
    shadowOpacity: 0.14,
    shadowRadius: 10,
    shadowOffset: { width: 0, height: 4 },
    elevation: 3
  },
  panelTitle: {
    color: palette.text,
    fontSize: 17,
    fontWeight: "900",
    letterSpacing: -0.3
  },
  listRow: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 12,
    paddingVertical: 8,
    borderBottomWidth: 1,
    borderColor: "rgba(255,255,255,0.06)"
  },
  dashboardActionRow: {
    minHeight: 54,
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 12,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(255,255,255,0.06)",
    backgroundColor: palette.surface2,
    paddingHorizontal: 10,
    paddingVertical: 9
  },
  profitLossHero: {
    minHeight: 118,
    justifyContent: "center",
    gap: 6,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    padding: 14
  },
  profitLossHeroLabel: {
    color: palette.muted,
    fontSize: 11,
    fontWeight: "900",
    textTransform: "uppercase"
  },
  profitLossHeroValue: {
    fontSize: 32,
    lineHeight: 36,
    fontWeight: "900"
  },
  profitLossHeroMeta: {
    color: palette.muted,
    fontSize: 12,
    fontWeight: "800"
  },
  profitLossRows: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 8
  },
  profitLossRow: {
    width: "48%",
    minHeight: 62,
    justifyContent: "space-between",
    gap: 6,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    padding: 10
  },
  profitLossRowLabel: {
    color: palette.muted,
    fontSize: 11,
    fontWeight: "800"
  },
  profitLossRowValue: {
    fontSize: 15,
    fontWeight: "900"
  },
  profitLossValuePositive: {
    color: palette.whatsapp
  },
  profitLossValueNegative: {
    color: palette.danger
  },
  dashboardQueueRow: {
    minHeight: 86,
    flexDirection: "row",
    alignItems: "center",
    gap: 10,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    padding: 10
  },
  dashboardQueueRowDanger: {
    borderColor: palette.danger,
    backgroundColor: "rgba(255,107,95,0.12)"
  },
  dashboardQueueRowWarning: {
    borderColor: palette.accentViolet,
    backgroundColor: `${palette.accentViolet}1f`
  },
  dashboardQueueRowSuccess: {
    borderColor: palette.whatsapp,
    backgroundColor: `${palette.whatsapp}1f`
  },
  dashboardQueueRank: {
    width: 38,
    height: 38,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    color: palette.accent,
    backgroundColor: palette.surface,
    textAlign: "center",
    textAlignVertical: "center",
    fontSize: 12,
    fontWeight: "900"
  },
  dashboardQueueCopy: {
    flex: 1,
    minWidth: 0,
    gap: 3
  },
  dashboardQueueLabel: {
    color: palette.accent,
    fontSize: 10,
    fontWeight: "900",
    textTransform: "uppercase"
  },
  dashboardQueueTitle: {
    color: palette.text,
    fontSize: 14,
    fontWeight: "900"
  },
  dashboardQueueDetail: {
    color: palette.muted,
    fontSize: 12,
    lineHeight: 17
  },
  dashboardQueueValue: {
    maxWidth: 96,
    color: palette.text,
    fontSize: 12,
    fontWeight: "900",
    textAlign: "right"
  },
  heatmapGrid: {
    gap: 10
  },
  heatmapTile: {
    minHeight: 188,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    padding: 12,
    gap: 11
  },
  heatmapTileGreen: {
    borderColor: "#66bb6a",
    backgroundColor: "rgba(102,187,106,0.12)"
  },
  heatmapTileYellow: {
    borderColor: "#ffb74d",
    backgroundColor: "rgba(255,183,77,0.12)"
  },
  heatmapTileRed: {
    borderColor: "#e57373",
    backgroundColor: "rgba(229,115,115,0.13)"
  },
  heatmapTileTopline: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 10
  },
  heatmapTileTitle: {
    flex: 1,
    minWidth: 0,
    color: palette.text,
    fontSize: 17,
    fontWeight: "900"
  },
  heatmapScore: {
    color: palette.muted,
    fontSize: 12,
    fontWeight: "900"
  },
  heatmapSignalRail: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 7
  },
  heatmapSignal: {
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    paddingHorizontal: 8,
    paddingVertical: 5
  },
  heatmapSignalGreen: {
    borderColor: "#66bb6a",
    backgroundColor: "rgba(102,187,106,0.18)"
  },
  heatmapSignalYellow: {
    borderColor: "#ffb74d",
    backgroundColor: "rgba(255,183,77,0.18)"
  },
  heatmapSignalRed: {
    borderColor: "#e57373",
    backgroundColor: "rgba(229,115,115,0.20)"
  },
  heatmapSignalText: {
    color: palette.text,
    fontSize: 11,
    fontWeight: "900"
  },
  heatmapMetricRow: {
    flexDirection: "row",
    gap: 10
  },
  heatmapMetricCell: {
    flex: 1,
    minWidth: 0,
    gap: 4
  },
  heatmapMetricLabel: {
    color: palette.muted,
    fontSize: 11,
    fontWeight: "800"
  },
  heatmapMetricValue: {
    color: palette.text,
    fontSize: 15,
    fontWeight: "900"
  },
  heatmapMetricMeta: {
    color: palette.muted,
    fontSize: 11,
    fontWeight: "800"
  },
  heatmapStockLine: {
    color: palette.muted,
    fontSize: 12,
    fontWeight: "800"
  },
  listRowCopy: {
    flex: 1
  },
  listRowTitle: {
    color: palette.text,
    fontWeight: "800"
  },
  listRowSubtitle: {
    color: palette.muted,
    marginTop: 3,
    fontSize: 12
  },
  listRowValue: {
    color: palette.text,
    fontWeight: "900",
    textAlign: "right"
  },
  inlineButtons: {
    flexDirection: "row",
    gap: 8,
    flexWrap: "wrap"
  },
  assistantActionGrid: {
    gap: 9
  },
  assistantActionButton: {
    minHeight: 76,
    justifyContent: "center",
    gap: 5,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    padding: 11
  },
  assistantActionButtonGood: {
    borderColor: palette.whatsapp,
    backgroundColor: "rgba(37,211,102,0.12)"
  },
  assistantActionButtonWarning: {
    borderColor: palette.danger,
    backgroundColor: "rgba(255,107,95,0.12)"
  },
  assistantActionTitle: {
    color: palette.text,
    fontSize: 13,
    fontWeight: "900"
  },
  assistantActionDescription: {
    color: palette.muted,
    fontSize: 12,
    lineHeight: 17
  },
  assistantActionKind: {
    color: palette.soft,
    fontSize: 10,
    fontWeight: "900",
    textTransform: "uppercase"
  },
  visualActionRow: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 8
  },
  visualPreviewFrame: {
    position: "relative",
    minHeight: 260,
    overflow: "hidden",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input
  },
  visualPreviewImage: {
    width: "100%",
    height: 300,
    resizeMode: "cover"
  },
  visualPreviewEmpty: {
    minHeight: 260,
    alignItems: "center",
    justifyContent: "center",
    gap: 6,
    padding: 18
  },
  visualPreviewEmptyTitle: {
    color: palette.text,
    fontSize: 16,
    fontWeight: "900",
    textAlign: "center"
  },
  visualPreviewEmptyText: {
    color: palette.muted,
    fontSize: 12,
    textAlign: "center"
  },
  visualPin: {
    position: "absolute",
    width: 36,
    height: 36,
    marginLeft: -18,
    marginTop: -18,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 18,
    borderWidth: 1,
    borderColor: palette.text,
    backgroundColor: palette.accent
  },
  visualPinText: {
    color: palette.text,
    fontSize: 12,
    fontWeight: "900"
  },
  segmentRail: {
    gap: 8,
    paddingVertical: 2
  },
  segmentButton: {
    minHeight: 38,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    paddingHorizontal: 12,
    alignItems: "center",
    justifyContent: "center"
  },
  segmentButtonActive: {
    borderColor: palette.accent,
    backgroundColor: palette.surface2
  },
  segmentText: {
    color: palette.muted,
    fontSize: 12,
    fontWeight: "800"
  },
  segmentTextActive: {
    color: palette.text
  },
  emptyState: {
    color: palette.muted,
    paddingVertical: 12
  },
  screenListFrame: {
    height: 320,
    overflow: "hidden",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input
  },
  screenListFrameLarge: {
    height: 460,
    overflow: "hidden",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input
  },
  screenListContent: {
    gap: 8,
    padding: 8
  },
  mechanicHero: {
    minHeight: 108,
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 12,
    overflow: "hidden",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    padding: 14
  },
  mechanicHeroCopy: {
    flex: 1,
    minWidth: 0,
    gap: 5
  },
  mechanicHeroTitle: {
    color: palette.text,
    fontSize: 24,
    lineHeight: 28,
    fontWeight: "900"
  },
  mechanicHeroMeta: {
    color: palette.muted,
    fontSize: 12,
    lineHeight: 17,
    fontWeight: "800"
  },
  mechanicHeroMark: {
    width: 82,
    height: 82,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.accent,
    backgroundColor: palette.input
  },
  mechanicHeroMarkText: {
    color: palette.text,
    fontSize: 24,
    fontWeight: "900"
  },
  mechanicHeroMarkLabel: {
    color: palette.accent,
    fontSize: 10,
    fontWeight: "900",
    textTransform: "uppercase"
  },
  mechanicResultStack: {
    gap: 5
  },
  mechanicPickerFrame: {
    height: 260,
    overflow: "hidden",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input
  },
  mechanicPartCard: {
    minHeight: 72,
    flexDirection: "row",
    alignItems: "center",
    gap: 12,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    padding: 10
  },
  mechanicPartMark: {
    width: 48,
    height: 48,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 2,
    backgroundColor: palette.accent
  },
  mechanicPartMarkText: {
    color: palette.text,
    fontSize: 16,
    fontWeight: "900"
  },
  mechanicPartCopy: {
    flex: 1,
    minWidth: 0,
    gap: 4
  },
  mechanicPartTitle: {
    color: palette.text,
    fontSize: 15,
    fontWeight: "900"
  },
  mechanicPartMeta: {
    color: palette.muted,
    fontSize: 12,
    lineHeight: 17
  },
  mechanicMetricRow: {
    flexDirection: "row",
    gap: 8
  },
  mechanicMetric: {
    flex: 1,
    minWidth: 0,
    minHeight: 72,
    justifyContent: "space-between",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input,
    padding: 10
  },
  mechanicMetricGood: {
    borderColor: palette.whatsapp,
    backgroundColor: "rgba(37,211,102,0.11)"
  },
  mechanicMetricLabel: {
    color: palette.muted,
    fontSize: 10,
    fontWeight: "900",
    textTransform: "uppercase"
  },
  mechanicMetricValue: {
    color: palette.text,
    fontSize: 20,
    fontWeight: "900"
  },
  mechanicWarehouseChip: {
    width: 154,
    minHeight: 58,
    justifyContent: "center",
    gap: 4,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input,
    paddingHorizontal: 10
  },
  mechanicWarehouseChipActive: {
    borderColor: palette.accent,
    backgroundColor: palette.surface2
  },
  mechanicWarehouseName: {
    color: palette.text,
    fontSize: 12,
    fontWeight: "900"
  },
  mechanicWarehouseNameActive: {
    color: palette.accent
  },
  mechanicWarehouseMeta: {
    color: palette.muted,
    fontSize: 11,
    fontWeight: "800"
  },
  mechanicPhotoFrame: {
    height: 228,
    overflow: "hidden",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input
  },
  mechanicPhoto: {
    width: "100%",
    height: "100%",
    resizeMode: "cover"
  },
  mechanicPhotoEmpty: {
    flex: 1,
    alignItems: "center",
    justifyContent: "center",
    gap: 6,
    padding: 18
  },
  mechanicPhotoEmptyTitle: {
    color: palette.text,
    fontSize: 16,
    fontWeight: "900"
  },
  mechanicPhotoEmptyText: {
    color: palette.muted,
    fontSize: 12,
    lineHeight: 17,
    textAlign: "center"
  },
  deadStockControls: {
    flexDirection: "row",
    gap: 10
  },
  deadStockControlCell: {
    flex: 1,
    minWidth: 0
  },
  deadStockMetricGrid: {
    flexDirection: "row",
    gap: 10
  },
  deadStockMetric: {
    flex: 1,
    minWidth: 0,
    minHeight: 86,
    justifyContent: "space-between",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    padding: 11
  },
  deadStockMetricLabel: {
    color: palette.muted,
    fontSize: 11,
    fontWeight: "800"
  },
  deadStockMetricValue: {
    color: palette.text,
    fontSize: 15,
    fontWeight: "900"
  },
  deadStockMetricDetail: {
    color: palette.accent,
    fontSize: 10,
    fontWeight: "900"
  },
  deadStockQueueFrame: {
    maxHeight: 430,
    overflow: "hidden",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input
  },
  deadStockQueueItem: {
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    padding: 11,
    gap: 7
  },
  deadStockQueueItemActive: {
    borderColor: palette.accent,
    backgroundColor: palette.surface2
  },
  deadStockQueueTop: {
    flexDirection: "row",
    alignItems: "flex-start",
    justifyContent: "space-between",
    gap: 10
  },
  deadStockPartName: {
    flex: 1,
    minWidth: 0,
    color: palette.text,
    fontWeight: "900"
  },
  deadStockDormant: {
    color: palette.accent,
    fontWeight: "900"
  },
  deadStockPartMeta: {
    color: palette.muted,
    fontSize: 12
  },
  deadStockQueueBottom: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 10
  },
  deadStockStockValue: {
    color: palette.text,
    fontSize: 12,
    fontWeight: "800"
  },
  deadStockActionPill: {
    maxWidth: "54%",
    color: palette.accent,
    fontSize: 11,
    fontWeight: "900",
    textAlign: "right"
  },
  deadStockSelectedHeader: {
    flexDirection: "row",
    alignItems: "flex-start",
    justifyContent: "space-between",
    gap: 10
  },
  deadStockSelectedCopy: {
    flex: 1,
    minWidth: 0
  },
  deadStockSelectedTitle: {
    color: palette.text,
    fontSize: 18,
    fontWeight: "900"
  },
  deadStockSelectedMeta: {
    color: palette.muted,
    fontSize: 12,
    marginTop: 4
  },
  deadStockSelectedBadge: {
    maxWidth: 116,
    color: palette.accent,
    fontSize: 11,
    fontWeight: "900",
    textAlign: "right"
  },
  deadStockFacts: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 8
  },
  deadStockFact: {
    minHeight: 30,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input,
    color: palette.text,
    fontSize: 11,
    fontWeight: "800",
    paddingHorizontal: 10,
    paddingVertical: 7
  },
  deadStockActions: {
    gap: 10
  },
  deadStockActionCard: {
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    padding: 11,
    gap: 7
  },
  deadStockActionHeader: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 8
  },
  deadStockActionTitle: {
    flex: 1,
    color: palette.text,
    fontWeight: "900"
  },
  deadStockActionTone: {
    color: palette.accent,
    fontSize: 11,
    fontWeight: "900"
  },
  deadStockActionDetail: {
    color: palette.muted,
    fontSize: 12,
    lineHeight: 17
  },
  compatMetricGrid: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 10
  },
  compatMetric: {
    width: "48%",
    minHeight: 78,
    justifyContent: "space-between",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    padding: 11
  },
  compatMetricLabel: {
    color: palette.muted,
    fontSize: 11,
    fontWeight: "800"
  },
  compatMetricValue: {
    color: palette.text,
    fontSize: 16,
    fontWeight: "900"
  },
  compatGraphFrame: {
    position: "relative",
    minHeight: 260,
    overflow: "hidden",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input
  },
  compatEmptyGraph: {
    minHeight: 220,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input,
    padding: 16
  },
  compatGraphOverlay: {
    position: "absolute",
    top: 0,
    right: 0,
    bottom: 0,
    left: 0,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: "rgba(16,17,20,0.62)",
    padding: 18,
    gap: 6
  },
  compatGraphOverlayTitle: {
    color: palette.text,
    fontSize: 16,
    fontWeight: "900",
    textAlign: "center"
  },
  compatGraphOverlayText: {
    color: palette.muted,
    fontSize: 12,
    lineHeight: 17,
    textAlign: "center"
  },
  compatPartRow: {
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "transparent",
    backgroundColor: palette.surface
  },
  compatPartRowActive: {
    borderColor: palette.accent,
    backgroundColor: palette.surface2
  },
  prepMetricGrid: {
    flexDirection: "row",
    gap: 10
  },
  prepMetric: {
    flex: 1,
    minWidth: 0,
    minHeight: 76,
    justifyContent: "space-between",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    padding: 11
  },
  prepMetricLabel: {
    color: palette.muted,
    fontSize: 11,
    fontWeight: "800"
  },
  prepMetricValue: {
    color: palette.text,
    fontSize: 18,
    fontWeight: "900"
  },
  prepBoardRail: {
    gap: 10,
    paddingVertical: 2,
    paddingRight: 4
  },
  prepColumn: {
    width: 248,
    height: 420,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input,
    padding: 10,
    gap: 9
  },
  prepColumnActive: {
    borderColor: palette.accent,
    backgroundColor: palette.surface
  },
  prepColumnHeader: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 8
  },
  prepColumnTitle: {
    color: palette.text,
    fontWeight: "900"
  },
  prepColumnCount: {
    minWidth: 26,
    height: 26,
    borderRadius: 3,
    overflow: "hidden",
    color: palette.muted,
    backgroundColor: palette.surface2,
    textAlign: "center",
    textAlignVertical: "center",
    fontWeight: "900"
  },
  prepColumnScroller: {
    flex: 1,
    minHeight: 0
  },
  prepColumnList: {
    gap: 9,
    paddingBottom: 4
  },
  prepCard: {
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    padding: 10,
    gap: 8
  },
  prepCardActive: {
    borderColor: palette.accent,
    backgroundColor: palette.surface2
  },
  prepCardAvatar: {
    width: 38,
    height: 38,
    borderRadius: 2,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: palette.accent
  },
  prepCardAvatarText: {
    color: palette.text,
    fontWeight: "900"
  },
  prepCardCopy: {
    minWidth: 0,
    gap: 3
  },
  prepCardTitle: {
    color: palette.text,
    fontWeight: "900"
  },
  prepCardMeta: {
    color: palette.muted,
    fontSize: 12
  },
  prepProgress: {
    height: 6,
    overflow: "hidden",
    borderRadius: 2,
    backgroundColor: palette.surface2
  },
  prepProgressFill: {
    height: "100%",
    borderRadius: 2,
    backgroundColor: palette.accent
  },
  prepCardFooter: {
    color: palette.muted,
    fontSize: 11,
    fontWeight: "800"
  },
  prepSelectedHeader: {
    flexDirection: "row",
    alignItems: "center",
    gap: 11
  },
  prepSelectedAvatar: {
    width: 48,
    height: 48,
    borderRadius: 2,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: palette.accent
  },
  prepSelectedAvatarText: {
    color: palette.text,
    fontSize: 17,
    fontWeight: "900"
  },
  prepSelectedCopy: {
    flex: 1,
    minWidth: 0
  },
  prepSelectedTitle: {
    color: palette.text,
    fontSize: 18,
    fontWeight: "900"
  },
  prepSelectedMeta: {
    color: palette.muted,
    fontSize: 12,
    marginTop: 3
  },
  prepProgressLarge: {
    height: 8,
    overflow: "hidden",
    borderRadius: 2,
    backgroundColor: palette.input
  },
  prepTaskRow: {
    minHeight: 54,
    flexDirection: "row",
    alignItems: "center",
    gap: 10,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input,
    paddingHorizontal: 10,
    paddingVertical: 8
  },
  prepTaskRowDone: {
    opacity: 0.72
  },
  prepTaskToggle: {
    width: 28,
    height: 28,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.accent,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: palette.surface
  },
  prepTaskToggleText: {
    color: palette.accent,
    fontWeight: "900"
  },
  prepTaskCopy: {
    flex: 1,
    minWidth: 0
  },
  prepTaskTitle: {
    color: palette.text,
    fontWeight: "900"
  },
  prepTaskMeta: {
    color: palette.muted,
    fontSize: 12,
    marginTop: 2
  },
  prepTaskDelete: {
    width: 32,
    height: 32,
    borderRadius: 2,
    alignItems: "center",
    justifyContent: "center",
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2
  },
  prepTaskDeleteText: {
    color: palette.text,
    fontWeight: "900"
  },
  reportHero: {
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    padding: 14,
    gap: 12
  },
  reportHeroTop: {
    flexDirection: "row",
    alignItems: "flex-start",
    justifyContent: "space-between",
    gap: 12
  },
  reportHeroCopy: {
    flex: 1,
    minWidth: 0,
    gap: 4
  },
  reportKicker: {
    color: palette.accent,
    fontSize: 10,
    fontWeight: "900",
    textTransform: "uppercase"
  },
  reportHeroTitle: {
    color: palette.text,
    fontSize: 24,
    fontWeight: "900"
  },
  reportHeroSubtitle: {
    color: palette.muted,
    fontSize: 13,
    fontWeight: "800"
  },
  reportHeroBadge: {
    minWidth: 92,
    minHeight: 54,
    alignItems: "flex-end",
    justifyContent: "center",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input,
    paddingHorizontal: 10
  },
  reportHeroBadgeText: {
    color: palette.text,
    fontSize: 16,
    fontWeight: "900"
  },
  reportHeroBadgeMeta: {
    color: palette.soft,
    fontSize: 10,
    fontWeight: "800",
    marginTop: 2
  },
  reportHeroMetaRow: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 8
  },
  reportHeroMeta: {
    color: palette.muted,
    fontSize: 11,
    fontWeight: "800",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    paddingHorizontal: 9,
    paddingVertical: 6
  },
  reportSummaryStrip: {
    gap: 8,
    paddingVertical: 2
  },
  reportSummaryCard: {
    width: 142,
    minHeight: 76,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    padding: 10,
    justifyContent: "space-between"
  },
  reportSummaryCardStrong: {
    borderColor: palette.accent,
    backgroundColor: palette.surface2
  },
  reportSummaryLabel: {
    color: palette.muted,
    fontSize: 10,
    fontWeight: "900",
    textTransform: "uppercase"
  },
  reportSummaryValue: {
    color: palette.text,
    fontSize: 14,
    fontWeight: "900",
    fontVariant: ["tabular-nums"]
  },
  reportDebitText: {
    color: palette.whatsapp
  },
  reportCreditText: {
    color: palette.danger
  },
  reportTools: {
    flexDirection: "row",
    flexWrap: "wrap",
    alignItems: "center",
    gap: 9
  },
  reportSearchInput: {
    flex: 1,
    minWidth: 170,
    minHeight: 42,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input,
    color: palette.text,
    paddingHorizontal: 12
  },
  reportToolButtons: {
    flexDirection: "row",
    gap: 8
  },
  reportToolButton: {
    minHeight: 42,
    minWidth: 78,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    paddingHorizontal: 12
  },
  reportToolButtonPrimary: {
    borderColor: palette.accent,
    backgroundColor: palette.accent
  },
  reportToolButtonText: {
    color: palette.text,
    fontSize: 12,
    fontWeight: "900"
  },
  reportToolButtonTextPrimary: {
    color: palette.text
  },
  reportTableFrame: {
    height: 520,
    overflow: "hidden",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input
  },
  reportHorizontalScroll: {
    flex: 1
  },
  reportTableWide: {
    height: "100%"
  },
  reportTableHeader: {
    minHeight: 42,
    flexDirection: "row",
    alignItems: "center",
    borderBottomWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2
  },
  reportVerticalScroll: {
    flex: 1
  },
  reportTableBody: {
    paddingBottom: 8
  },
  reportTableRow: {
    minHeight: 58,
    flexDirection: "row",
    alignItems: "stretch",
    borderBottomWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input
  },
  reportTableRowActive: {
    backgroundColor: palette.surface
  },
  reportCell: {
    justifyContent: "center",
    paddingHorizontal: 9,
    paddingVertical: 8,
    borderRightWidth: 1,
    borderColor: palette.line
  },
  reportCellText: {
    color: palette.text,
    fontSize: 11,
    fontWeight: "800"
  },
  reportHeaderText: {
    color: palette.muted,
    fontSize: 10,
    fontWeight: "900",
    textTransform: "uppercase"
  },
  reportNumericText: {
    textAlign: "right",
    fontVariant: ["tabular-nums"]
  },
  reportCellAccount: {
    width: 210
  },
  reportCellType: {
    width: 118
  },
  reportCellDate: {
    width: 92
  },
  reportCellReference: {
    width: 124
  },
  reportCellDescription: {
    width: 250
  },
  reportCellMoney: {
    width: 116,
    alignItems: "flex-end"
  },
  reportExpanded: {
    borderBottomWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    padding: 10
  },
  reportDetailGrid: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 8
  },
  reportDetailTile: {
    minWidth: 132,
    flex: 1,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input,
    padding: 9
  },
  reportDetailLabel: {
    color: palette.soft,
    fontSize: 10,
    fontWeight: "900",
    textTransform: "uppercase"
  },
  reportDetailValue: {
    color: palette.text,
    fontSize: 12,
    fontWeight: "900",
    marginTop: 4
  },
  screenListItem: {
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    padding: 10,
    gap: 8
  },
  adminGroup: {
    gap: 8
  },
  adminGroupTitle: {
    color: palette.muted,
    fontSize: 11,
    fontWeight: "900",
    textTransform: "uppercase"
  },
  adminLaunchGrid: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 8
  },
  adminLaunchButton: {
    width: "48%",
    minHeight: 58,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    padding: 10,
    justifyContent: "space-between"
  },
  adminLaunchButtonActive: {
    borderColor: palette.accent
  },
  adminLaunchText: {
    color: palette.text,
    fontSize: 12,
    fontWeight: "900"
  },
  adminLaunchTextActive: {
    color: palette.accent
  },
  adminLaunchMeta: {
    color: palette.soft,
    fontSize: 10,
    fontWeight: "800",
    textTransform: "uppercase"
  },
  managementWorkspaceGrid: {
    gap: 14
  },
  managementListFrame: {
    height: 360,
    overflow: "hidden",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input
  },
  managementListContent: {
    gap: 8,
    padding: 8
  },
  adminCrudPanel: {
    gap: 12
  },
  adminCrudGrid: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 10
  },
  adminCrudField: {
    flex: 1,
    minWidth: 140,
    gap: 6
  },
  adminCrudToggle: {
    flex: 1,
    minWidth: 140,
    minHeight: 46,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    paddingHorizontal: 10,
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between"
  },
  adminCrudToggleText: {
    color: palette.text,
    fontSize: 12,
    fontWeight: "900"
  },
  adminCrudRow: {
    minHeight: 58,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    padding: 10,
    flexDirection: "row",
    alignItems: "center",
    gap: 10
  },
  adminCrudRowActive: {
    borderColor: palette.accent,
    backgroundColor: palette.surface
  },
  adminCrudRowCopy: {
    flex: 1,
    minWidth: 0
  },
  adminCrudRowTitleActive: {
    color: palette.accent
  },
  usedCarHero: {
    gap: 12
  },
  usedCarWorkspaceGrid: {
    gap: 14
  },
  usedCarWorkspaceGridWide: {
    flexDirection: "row",
    alignItems: "flex-start"
  },
  usedCarWorkspacePrimary: {
    gap: 14
  },
  usedCarWorkspacePrimaryWide: {
    flex: 1.25
  },
  usedCarWorkspaceSide: {
    gap: 14
  },
  usedCarWorkspaceSideWide: {
    flex: 0.9
  },
  usedCarGalleryFrame: {
    height: 300,
    overflow: "hidden",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2
  },
  usedCarHeroImage: {
    width: "100%",
    height: "100%"
  },
  usedCarHeroPlaceholder: {
    flex: 1,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: palette.input
  },
  usedCarHeroInitials: {
    color: palette.accent,
    fontSize: 40,
    fontWeight: "900"
  },
  usedCarHeroCopy: {
    gap: 7
  },
  usedCarHeroTitle: {
    color: palette.text,
    fontSize: 24,
    fontWeight: "900"
  },
  usedCarHeroSubtitle: {
    color: palette.muted,
    fontSize: 13,
    fontWeight: "800"
  },
  usedCarBadgeRow: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 7
  },
  usedCarBadge: {
    minHeight: 28,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    alignItems: "center",
    justifyContent: "center",
    paddingHorizontal: 9
  },
  usedCarBadgeText: {
    color: palette.text,
    fontSize: 11,
    fontWeight: "900"
  },
  usedCarCockpit: {
    gap: 10
  },
  usedCarMetricGrid: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 8
  },
  usedCarMetricTile: {
    flex: 1,
    minWidth: 148,
    minHeight: 92,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    padding: 10,
    gap: 5
  },
  usedCarMetricPositive: {
    borderColor: palette.whatsapp
  },
  usedCarMetricWarning: {
    borderColor: palette.accentViolet
  },
  usedCarMetricDanger: {
    borderColor: palette.danger
  },
  usedCarMetricLabel: {
    color: palette.muted,
    fontSize: 10,
    fontWeight: "900",
    textTransform: "uppercase"
  },
  usedCarMetricValue: {
    color: palette.text,
    fontSize: 22,
    fontWeight: "900"
  },
  usedCarMetricMeta: {
    color: palette.muted,
    fontSize: 11,
    fontWeight: "800",
    lineHeight: 15
  },
  usedCarFilterRail: {
    gap: 7,
    paddingVertical: 1
  },
  usedCarFocusList: {
    gap: 8,
    borderTopWidth: 1,
    borderTopColor: palette.line,
    paddingTop: 10
  },
  usedCarFocusCard: {
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    padding: 10,
    gap: 5
  },
  usedCarFocusTop: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 8
  },
  usedCarActionPill: {
    minHeight: 26,
    maxWidth: "70%",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input,
    color: palette.text,
    paddingHorizontal: 8,
    paddingTop: 5,
    fontSize: 10,
    fontWeight: "900",
    textTransform: "uppercase",
    overflow: "hidden"
  },
  usedCarActionPill_positive: {
    borderColor: palette.whatsapp,
    color: palette.whatsapp
  },
  usedCarActionPill_success: {
    borderColor: palette.whatsapp,
    color: palette.whatsapp
  },
  usedCarActionPill_warning: {
    borderColor: palette.accentViolet,
    color: palette.accentViolet
  },
  usedCarActionPill_danger: {
    borderColor: palette.danger,
    color: palette.danger
  },
  usedCarFocusGap: {
    color: palette.accent,
    fontSize: 12,
    fontWeight: "900",
    textAlign: "right"
  },
  usedCarFocusTitle: {
    color: palette.text,
    fontSize: 14,
    fontWeight: "900"
  },
  usedCarThumbRail: {
    gap: 8,
    paddingVertical: 2
  },
  usedCarThumb: {
    width: 66,
    height: 50,
    overflow: "hidden",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    alignItems: "center",
    justifyContent: "center"
  },
  usedCarThumbActive: {
    borderColor: palette.accent
  },
  usedCarThumbImage: {
    width: "100%",
    height: "100%"
  },
  usedCarListingOverlay: {
    flex: 1,
    justifyContent: "flex-end",
    backgroundColor: "rgba(0,0,0,0.5)"
  },
  usedCarListingCard: {
    maxHeight: "80%",
    borderTopLeftRadius: 18,
    borderTopRightRadius: 18,
    backgroundColor: palette.surface,
    paddingHorizontal: 16,
    paddingTop: 16
  },
  usedCarListingHeader: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    marginBottom: 10
  },
  usedCarListingScroll: {
    flexGrow: 0
  },
  usedCarFullscreenRoot: {
    flex: 1,
    backgroundColor: "#050505"
  },
  usedCarFullscreenHeader: {
    minHeight: 58,
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 12,
    paddingHorizontal: 14
  },
  usedCarFullscreenHeaderCopy: {
    flex: 1,
    minWidth: 0
  },
  usedCarFullscreenTitle: {
    color: "#ffffff",
    fontSize: 16,
    fontWeight: "900"
  },
  usedCarFullscreenMeta: {
    color: "rgba(255,255,255,0.62)",
    fontSize: 12,
    fontWeight: "800",
    marginTop: 2
  },
  usedCarFullscreenClose: {
    minHeight: 38,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(255,255,255,0.22)",
    backgroundColor: "rgba(255,255,255,0.1)",
    alignItems: "center",
    justifyContent: "center",
    paddingHorizontal: 12
  },
  usedCarFullscreenCloseText: {
    color: "#ffffff",
    fontSize: 12,
    fontWeight: "900"
  },
  usedCarFullscreenSlide: {
    alignItems: "center",
    justifyContent: "center",
    paddingHorizontal: 12
  },
  usedCarFullscreenZoomSurface: {
    width: "100%",
    height: "100%",
    overflow: "hidden",
    alignItems: "center",
    justifyContent: "center"
  },
  usedCarFullscreenImage: {
    width: "100%",
    height: "100%"
  },
  usedCarFullscreenPlaceholder: {
    width: "100%",
    height: "100%",
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 2,
    backgroundColor: "rgba(255,255,255,0.08)"
  },
  usedCarFullscreenHint: {
    color: "rgba(255,255,255,0.64)",
    fontSize: 12,
    fontWeight: "800",
    textAlign: "center",
    paddingHorizontal: 14,
    paddingVertical: 8
  },
  usedCarZoomToolbar: {
    minHeight: 42,
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "center",
    gap: 8,
    paddingHorizontal: 14
  },
  usedCarZoomButton: {
    width: 42,
    height: 38,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(255,255,255,0.22)",
    backgroundColor: "rgba(255,255,255,0.1)",
    alignItems: "center",
    justifyContent: "center"
  },
  usedCarZoomButtonText: {
    color: "#ffffff",
    fontSize: 20,
    fontWeight: "900"
  },
  usedCarZoomReadout: {
    minWidth: 72,
    height: 38,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(255,255,255,0.22)",
    backgroundColor: "rgba(255,255,255,0.08)",
    alignItems: "center",
    justifyContent: "center",
    paddingHorizontal: 10
  },
  usedCarZoomReadoutText: {
    color: "#ffffff",
    fontSize: 12,
    fontWeight: "900"
  },
  usedCarZoomReset: {
    minWidth: 70,
    height: 38,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(255,255,255,0.22)",
    backgroundColor: "rgba(255,255,255,0.1)",
    alignItems: "center",
    justifyContent: "center",
    paddingHorizontal: 10
  },
  usedCarZoomResetText: {
    color: "#ffffff",
    fontSize: 12,
    fontWeight: "900"
  },
  usedCarFullscreenThumbRail: {
    gap: 8,
    paddingHorizontal: 14,
    paddingTop: 8,
    paddingBottom: 2
  },
  usedCarFullscreenThumb: {
    width: 58,
    height: 44,
    overflow: "hidden",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(255,255,255,0.2)",
    backgroundColor: "rgba(255,255,255,0.08)",
    alignItems: "center",
    justifyContent: "center"
  },
  usedCarFullscreenThumbActive: {
    borderColor: palette.accent
  },
  usedCarCardGrid: {
    gap: 10,
    paddingVertical: 2
  },
  usedCarCard: {
    width: 154,
    gap: 8,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    padding: 10
  },
  usedCarCardActive: {
    borderColor: palette.accent
  },
  usedCarCardPlaceholder: {
    height: 82,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 2,
    backgroundColor: palette.input
  },
  usedCarCardTitle: {
    color: palette.text,
    fontSize: 13,
    fontWeight: "900"
  },
  usedCarCardMeta: {
    color: palette.muted,
    fontSize: 11,
    fontWeight: "800"
  },
  usedCarDetailGrid: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 8
  },
  usedCarDetailTile: {
    width: "48%",
    minHeight: 66,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    padding: 10,
    gap: 5
  },
  usedCarDetailLabel: {
    color: palette.muted,
    fontSize: 11,
    fontWeight: "800",
    textTransform: "uppercase"
  },
  usedCarDetailValue: {
    color: palette.text,
    fontSize: 13,
    fontWeight: "900"
  },
  usedCarField: {
    flex: 1,
    minWidth: 130,
    gap: 6
  },
  usedCarToggle: {
    flex: 1,
    minWidth: 130,
    minHeight: 46,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    paddingHorizontal: 10,
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between"
  },
  usedCarToggleText: {
    color: palette.text,
    fontSize: 12,
    fontWeight: "900"
  },
  usedCarSelector: {
    gap: 8
  },
  usedCarChipWrap: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 7
  },
  usedCarSelectChip: {
    maxWidth: "100%",
    minHeight: 40,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    paddingHorizontal: 10,
    paddingVertical: 7,
    justifyContent: "center"
  },
  usedCarSelectChipActive: {
    borderColor: palette.accent,
    backgroundColor: palette.input
  },
  usedCarSelectChipText: {
    color: palette.text,
    fontSize: 12,
    fontWeight: "900"
  },
  usedCarSelectChipTextActive: {
    color: palette.accent
  },
  usedCarSelectChipMeta: {
    color: palette.soft,
    fontSize: 10,
    fontWeight: "800",
    marginTop: 2
  },
  usedCarHelpText: {
    color: palette.muted,
    fontSize: 12,
    lineHeight: 17
  },
  usedCarFormGrid: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 10
  },
  usedCarCurrencyRow: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 7
  },
  usedCarCurrencyChip: {
    minHeight: 34,
    minWidth: 58,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    paddingHorizontal: 10
  },
  usedCarDangerSoft: {
    borderColor: palette.danger
  },
  usedCarInventoryList: {
    gap: 8,
    padding: 8
  },
  usedCarInventoryListFrame: {
    height: 370,
    overflow: "hidden",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input
  },
  usedCarInventoryRow: {
    minHeight: 68,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    padding: 10,
    flexDirection: "row",
    alignItems: "center",
    gap: 10
  },
  usedCarInventoryRowActive: {
    borderColor: palette.accent,
    backgroundColor: palette.input
  },
  usedCarInventoryMark: {
    width: 42,
    height: 42,
    borderRadius: 2,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: palette.surface
  },
  usedCarInventoryMarkText: {
    color: palette.accent,
    fontSize: 14,
    fontWeight: "900"
  },
  usedCarInventoryCopy: {
    flex: 1,
    minWidth: 0,
    gap: 4
  },
  usedCarInventoryValue: {
    color: palette.text,
    fontSize: 12,
    fontWeight: "900",
    textAlign: "right"
  },
  usedCarInventoryAction: {
    color: palette.muted,
    fontSize: 10,
    fontWeight: "900",
    textTransform: "uppercase"
  },
  usedCarInventoryAction_positive: {
    color: palette.whatsapp
  },
  usedCarInventoryAction_success: {
    color: palette.whatsapp
  },
  usedCarInventoryAction_warning: {
    color: palette.accentViolet
  },
  usedCarInventoryAction_danger: {
    color: palette.danger
  },
  usedCarRecoveryPanel: {
    gap: 10
  },
  usedCarProgressTrack: {
    height: 10,
    overflow: "hidden",
    borderRadius: 2,
    backgroundColor: palette.input
  },
  usedCarProgressFill: {
    height: "100%",
    borderRadius: 2,
    backgroundColor: palette.accent
  },
  usedCarPushParts: {
    gap: 8
  },
  usedCarPushPart: {
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    padding: 10,
    gap: 3
  },
  usedCarPartsPanel: {
    gap: 9
  },
  usedCarPartsListFrame: {
    height: 210,
    overflow: "hidden",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input
  },
  usedCarPartsListContent: {
    gap: 8,
    padding: 8
  },
  usedCarPartSectionTitle: {
    color: palette.muted,
    fontSize: 11,
    fontWeight: "900",
    textTransform: "uppercase"
  },
  usedCarPartRow: {
    minHeight: 58,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    padding: 10,
    flexDirection: "row",
    alignItems: "center",
    gap: 10
  },
  usedCarPartCopy: {
    flex: 1,
    minWidth: 0,
    gap: 3
  },
  usedCarPartTitle: {
    color: palette.text,
    fontSize: 12,
    fontWeight: "900"
  },
  usedCarPartMeta: {
    color: palette.muted,
    fontSize: 11,
    lineHeight: 15
  },
  usedCarMiniButton: {
    minHeight: 32,
    minWidth: 70,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    alignItems: "center",
    justifyContent: "center",
    paddingHorizontal: 9
  },
  usedCarMiniButtonText: {
    color: palette.text,
    fontSize: 11,
    fontWeight: "900"
  },
  newChatPanel: {
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    padding: 12,
    gap: 8
  },
  conversationRail: {
    gap: 10,
    paddingVertical: 2
  },
  conversationPill: {
    width: 180,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    padding: 10,
    gap: 6
  },
  conversationPillActive: {
    borderColor: palette.whatsapp,
    backgroundColor: "rgba(37,211,102,0.12)"
  },
  avatar: {
    width: 34,
    height: 34,
    borderRadius: 17,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: palette.whatsapp
  },
  avatarText: {
    color: palette.text,
    fontWeight: "900"
  },
  conversationName: {
    color: palette.text,
    fontWeight: "900"
  },
  conversationPreview: {
    color: palette.muted,
    fontSize: 12
  },
  threadList: {
    flex: 1,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface
  },
  threadContent: {
    gap: 10,
    padding: 12
  },
  messageBubble: {
    maxWidth: "86%",
    borderRadius: 2,
    borderWidth: 1,
    padding: 10,
    gap: 7
  },
  messageBubbleInbound: {
    alignSelf: "flex-start",
    borderColor: palette.line,
    backgroundColor: palette.surface2
  },
  messageBubbleOutbound: {
    alignSelf: "flex-end",
    borderColor: palette.whatsapp,
    backgroundColor: "rgba(37,211,102,0.16)"
  },
  messageBody: {
    color: palette.text,
    lineHeight: 20
  },
  messageMeta: {
    color: palette.muted,
    fontSize: 10
  },
  composer: {
    flexDirection: "row",
    gap: 10,
    alignItems: "flex-end"
  },
  composerInput: {
    flex: 1,
    minHeight: 48,
    maxHeight: 110,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input,
    color: palette.text,
    paddingHorizontal: 12,
    paddingVertical: 10
  },
  whatsAppCampaignContent: {
    gap: 12,
    padding: 12
  },
  whatsAppToggle: {
    minHeight: 44,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    paddingHorizontal: 12,
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 10
  },
  whatsAppToggleActive: {
    borderColor: palette.whatsapp,
    backgroundColor: "rgba(37,211,102,0.12)"
  },
  whatsAppToggleText: {
    color: palette.text,
    fontSize: 12,
    fontWeight: "900"
  },
  whatsAppToggleValue: {
    color: palette.muted,
    fontSize: 12,
    fontWeight: "900"
  },
  whatsAppCampaignStats: {
    flexDirection: "row",
    gap: 8
  },
  whatsAppCampaignStat: {
    flex: 1,
    minHeight: 62,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    padding: 10,
    justifyContent: "center",
    gap: 4
  },
  whatsAppCampaignAsset: {
    minHeight: 68,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    padding: 10,
    flexDirection: "row",
    alignItems: "center",
    gap: 10
  },
  whatsAppCampaignAssetActive: {
    borderColor: palette.whatsapp,
    backgroundColor: "rgba(37,211,102,0.12)"
  },
  whatsAppCampaignMessage: {
    minHeight: 190,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input,
    color: palette.text,
    textAlignVertical: "top",
    paddingHorizontal: 12,
    paddingVertical: 10,
    lineHeight: 20
  },
  storeRoot: {
    flex: 1,
    backgroundColor: "#08090b"
  },
  storeScroll: {
    flex: 1,
    minHeight: 0,
    backgroundColor: "#08090b"
  },
  storeScrollContent: {
    flexGrow: 1,
    paddingBottom: 26
  },
  storeHeader: {
    minHeight: 72,
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 12,
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderColor: "rgba(247,244,237,0.13)",
    backgroundColor: "#08090b"
  },
  storeBrand: {
    flex: 1,
    minWidth: 0,
    flexDirection: "row",
    alignItems: "center",
    gap: 12
  },
  storeBrandMark: {
    width: 40,
    height: 40,
    borderRadius: 2,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: "#d71920"
  },
  storeBrandMarkText: {
    color: "#fff",
    fontWeight: "900",
    fontSize: 15
  },
  storeBrandCopy: {
    flex: 1,
    minWidth: 0
  },
  storeBrandTitle: {
    color: "#f7f4ed",
    fontWeight: "900",
    fontSize: 15
  },
  storeBrandSubtitle: {
    color: "#b6b0a5",
    fontSize: 11,
    marginTop: 2
  },
  storeLogoutButton: {
    minHeight: 36,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(247,244,237,0.13)",
    alignItems: "center",
    justifyContent: "center",
    paddingHorizontal: 10
  },
  storeLogoutText: {
    color: "#f7f4ed",
    fontWeight: "900",
    fontSize: 12
  },
  storeHero: {
    minHeight: 560,
    justifyContent: "flex-end",
    backgroundColor: "#111316"
  },
  storeHeroImage: {
    opacity: 0.9
  },
  storeHeroOverlay: {
    minHeight: 560,
    justifyContent: "flex-end",
    gap: 16,
    padding: 18,
    backgroundColor: "rgba(8,9,11,0.58)"
  },
  storeEyebrow: {
    color: "#d6a94f",
    fontSize: 11,
    fontWeight: "900",
    textTransform: "uppercase"
  },
  storeHeroTitle: {
    color: "#f7f4ed",
    fontSize: 48,
    lineHeight: 48,
    fontWeight: "900"
  },
  storeHeroBody: {
    color: "#d8d2c8",
    fontSize: 16,
    lineHeight: 24
  },
  storeHeroStats: {
    flexDirection: "row",
    gap: 1,
    overflow: "hidden",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(247,244,237,0.13)",
    backgroundColor: "rgba(247,244,237,0.13)"
  },
  storeHeroStat: {
    flex: 1,
    minHeight: 74,
    justifyContent: "center",
    gap: 4,
    paddingHorizontal: 10,
    backgroundColor: "rgba(8,9,11,0.76)"
  },
  storeHeroStatValue: {
    color: "#f7f4ed",
    fontSize: 21,
    fontWeight: "900"
  },
  storeHeroStatLabel: {
    color: "#b6b0a5",
    fontSize: 11,
    fontWeight: "800"
  },
  storeHeroActions: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 10
  },
  storeActionGrid: {
    gap: 10,
    paddingHorizontal: 16,
    paddingTop: 16,
    backgroundColor: "#0e1013"
  },
  storeActionButton: {
    minHeight: 88,
    gap: 5,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(247,244,237,0.13)",
    backgroundColor: "#111419",
    padding: 12
  },
  storeActionButtonPrimary: {
    borderColor: "rgba(214,169,79,0.44)",
    backgroundColor: "rgba(214,169,79,0.1)"
  },
  storeActionValue: {
    color: "#d6a94f",
    fontSize: 12,
    fontWeight: "900"
  },
  storeActionTitle: {
    color: "#f7f4ed",
    fontSize: 18,
    fontWeight: "900"
  },
  storeActionSubtitle: {
    color: "#b6b0a5",
    fontSize: 12,
    lineHeight: 17
  },
  storeFitment: {
    gap: 10,
    padding: 16,
    borderBottomWidth: 1,
    borderColor: "rgba(247,244,237,0.13)",
    backgroundColor: "#0e1013"
  },
  storeFitmentLabel: {
    color: "#7d7a72",
    fontSize: 11,
    fontWeight: "900",
    textTransform: "uppercase"
  },
  storeFitmentTitle: {
    color: "#f7f4ed",
    fontSize: 20,
    fontWeight: "900"
  },
  storeBrandRail: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 8
  },
  storeBrandChip: {
    minHeight: 36,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(247,244,237,0.13)",
    paddingHorizontal: 11
  },
  storeBrandChipText: {
    color: "#b6b0a5",
    fontSize: 12,
    fontWeight: "900"
  },
  storeCatalogHeader: {
    gap: 14,
    padding: 16,
    paddingTop: 28
  },
  storeSectionTitle: {
    color: "#f7f4ed",
    fontSize: 34,
    lineHeight: 36,
    fontWeight: "900",
    marginTop: 7
  },
  storeSearchRow: {
    gap: 10
  },
  storeStatus: {
    color: "#b6b0a5",
    fontSize: 12,
    paddingHorizontal: 16
  },
  storePartGrid: {
    gap: 14,
    paddingHorizontal: 16
  },
  storePartCard: {
    overflow: "hidden",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(247,244,237,0.13)",
    backgroundColor: "#111316"
  },
  storePartImage: {
    height: 132,
    justifyContent: "center",
    alignItems: "center"
  },
  storePartImageInner: {
    opacity: 0.72
  },
  storePartImageOverlay: {
    width: "100%",
    height: "100%",
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: "rgba(215,25,32,0.42)"
  },
  storePartBadge: {
    width: 64,
    height: 64,
    textAlign: "center",
    textAlignVertical: "center",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(255,255,255,0.38)",
    backgroundColor: "rgba(8,9,11,0.58)",
    color: "#fff",
    fontSize: 22,
    fontWeight: "900"
  },
  storePartCopy: {
    gap: 8,
    padding: 14
  },
  storePartCode: {
    color: "#d6a94f",
    fontSize: 11,
    fontWeight: "900",
    textTransform: "uppercase"
  },
  storePartTitle: {
    color: "#f7f4ed",
    fontSize: 20,
    lineHeight: 24,
    fontWeight: "900"
  },
  storePartFitment: {
    color: "#b6b0a5",
    fontSize: 13,
    lineHeight: 19
  },
  storePartMeta: {
    flexDirection: "row",
    alignItems: "flex-end",
    justifyContent: "space-between",
    gap: 10,
    marginHorizontal: 14,
    paddingTop: 12,
    borderTopWidth: 1,
    borderColor: "rgba(247,244,237,0.13)"
  },
  storePartPrice: {
    color: "#f7f4ed",
    fontSize: 22,
    fontWeight: "900"
  },
  storePartAvailability: {
    color: "#b6b0a5",
    fontSize: 12,
    fontWeight: "800"
  },
  storePartInCart: {
    marginHorizontal: 14,
    marginTop: 10,
    alignSelf: "flex-start",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(214,169,79,0.38)",
    backgroundColor: "rgba(214,169,79,0.1)",
    paddingHorizontal: 9,
    paddingVertical: 6
  },
  storePartInCartText: {
    color: "#d6a94f",
    fontSize: 11,
    fontWeight: "900"
  },
  storeButton: {
    minHeight: 46,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 2,
    backgroundColor: "#d71920",
    margin: 14,
    paddingHorizontal: 14
  },
  storeButtonCompact: {
    flex: 1,
    margin: 0,
    minHeight: 40,
    paddingHorizontal: 10
  },
  storeButtonSecondary: {
    margin: 0,
    borderWidth: 1,
    borderColor: "rgba(247,244,237,0.13)",
    backgroundColor: "rgba(255,255,255,0.08)"
  },
  storeButtonText: {
    color: "#fff",
    fontWeight: "900"
  },
  storeEmpty: {
    gap: 6,
    paddingVertical: 22
  },
  storeEmptyTitle: {
    color: "#f7f4ed",
    fontSize: 20,
    fontWeight: "900"
  },
  storeEmptyText: {
    color: "#b6b0a5",
    fontSize: 13,
    lineHeight: 19
  },
  storeCheckout: {
    gap: 14,
    marginHorizontal: 16,
    marginTop: 24,
    padding: 14,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(247,244,237,0.13)",
    backgroundColor: "#0f1114"
  },
  storeCartHeading: {
    flexDirection: "row",
    justifyContent: "space-between",
    gap: 12,
    alignItems: "center"
  },
  storeCartTitleLarge: {
    color: "#f7f4ed",
    fontSize: 30,
    fontWeight: "900"
  },
  storeCartCount: {
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(247,244,237,0.13)",
    color: "#d6a94f",
    fontSize: 12,
    fontWeight: "900",
    paddingHorizontal: 10,
    paddingVertical: 8,
    textTransform: "uppercase"
  },
  storeCartList: {
    gap: 10
  },
  storeCartRow: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 12,
    paddingBottom: 10,
    borderBottomWidth: 1,
    borderColor: "rgba(247,244,237,0.13)"
  },
  storeCartCopy: {
    flex: 1,
    minWidth: 0
  },
  storeCartTitle: {
    color: "#f7f4ed",
    fontWeight: "900"
  },
  storeCartSubtitle: {
    color: "#b6b0a5",
    marginTop: 3,
    fontSize: 12
  },
  storeStepper: {
    flexDirection: "row",
    alignItems: "center",
    overflow: "hidden",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(247,244,237,0.13)"
  },
  storeStepperButton: {
    width: 34,
    height: 34,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: "#1a1d22"
  },
  storeStepperText: {
    color: "#f7f4ed",
    fontSize: 18,
    fontWeight: "900"
  },
  storeStepperValue: {
    width: 38,
    color: "#f7f4ed",
    textAlign: "center",
    fontWeight: "900"
  },
  storeCheckoutSection: {
    gap: 10,
    padding: 12,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(247,244,237,0.13)",
    backgroundColor: "rgba(255,255,255,0.03)"
  },
  storeCheckoutSectionTitle: {
    color: "#f7f4ed",
    fontSize: 15,
    fontWeight: "900"
  },
  storePaymentGrid: {
    flexDirection: "row",
    gap: 8
  },
  storePaymentOption: {
    flex: 1,
    minHeight: 78,
    gap: 4,
    padding: 10,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(247,244,237,0.13)",
    backgroundColor: "#111419"
  },
  storePaymentOptionActive: {
    borderColor: "#d6a94f",
    backgroundColor: "rgba(214,169,79,0.11)"
  },
  storePaymentTitle: {
    color: "#f7f4ed",
    fontSize: 15,
    fontWeight: "900"
  },
  storePaymentSubtitle: {
    color: "#b6b0a5",
    fontSize: 12,
    lineHeight: 17
  },
  storePaymentNote: {
    color: "#b6b0a5",
    fontSize: 12,
    lineHeight: 18
  },
  storeTotalRow: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    borderTopWidth: 1,
    borderColor: "rgba(247,244,237,0.13)",
    paddingTop: 12
  },
  storeTotalLabel: {
    color: "#b6b0a5",
    fontWeight: "800"
  },
  storeTotalValue: {
    color: "#f7f4ed",
    fontSize: 24,
    fontWeight: "900"
  },
  storeSummaryBar: {
    marginHorizontal: 16,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(214,169,79,0.28)",
    backgroundColor: "rgba(214,169,79,0.08)",
    padding: 12,
    gap: 10
  },
  storeSummaryCopy: {
    gap: 3
  },
  storeSummaryLabel: {
    color: "#d6a94f",
    fontSize: 11,
    fontWeight: "900",
    textTransform: "uppercase"
  },
  storeSummaryValue: {
    color: "#f7f4ed",
    fontSize: 16,
    fontWeight: "900"
  },
  storeSummaryActions: {
    flexDirection: "row",
    gap: 8
  },
  storeStepRail: {
    flexDirection: "row",
    gap: 8
  },
  storeStep: {
    flex: 1,
    minHeight: 42,
    flexDirection: "row",
    alignItems: "center",
    gap: 7,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(247,244,237,0.13)",
    backgroundColor: "rgba(255,255,255,0.03)",
    paddingHorizontal: 8
  },
  storeStepActive: {
    borderColor: "rgba(214,169,79,0.4)",
    backgroundColor: "rgba(214,169,79,0.1)"
  },
  storeStepNumber: {
    width: 22,
    height: 22,
    borderRadius: 2,
    color: "#b6b0a5",
    textAlign: "center",
    textAlignVertical: "center",
    fontSize: 11,
    fontWeight: "900",
    backgroundColor: "rgba(255,255,255,0.08)"
  },
  storeStepNumberActive: {
    color: "#14100a",
    backgroundColor: "#d6a94f"
  },
  storeStepLabel: {
    flex: 1,
    color: "#b6b0a5",
    fontSize: 11,
    fontWeight: "900"
  },
  storeStepLabelActive: {
    color: "#f7f4ed"
  },
  storeCartActions: {
    flexDirection: "row",
    gap: 8
  },
  storeOrderSummaryRow: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 10,
    paddingBottom: 8,
    borderBottomWidth: 1,
    borderColor: "rgba(247,244,237,0.1)"
  },
  storeOrderSummaryName: {
    flex: 1,
    minWidth: 0,
    color: "#f7f4ed",
    fontSize: 13,
    fontWeight: "900"
  },
  storeOrderSummaryValue: {
    color: "#d6a94f",
    fontSize: 12,
    fontWeight: "900"
  },
  storeUserFooter: {
    flexDirection: "row",
    alignItems: "center",
    gap: 10,
    padding: 12,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(247,244,237,0.13)"
  },
  storeAvatar: {
    width: 36,
    height: 36,
    borderRadius: 18,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: "#d6a94f"
  },
  storeAvatarText: {
    color: "#14100a",
    fontWeight: "900"
  },
  storeUserCopy: {
    flex: 1,
    minWidth: 0
  },
  storeUserName: {
    color: "#f7f4ed",
    fontWeight: "900"
  },
  storeUserRole: {
    color: "#b6b0a5",
    fontSize: 12,
    marginTop: 2
  },

  /* ── Design System v2 additions ── */

  /* KPI Strip */
  kpiStrip: {
    flexDirection: "row",
    gap: 10,
    paddingHorizontal: 14,
    paddingVertical: 10
  },
  kpiCard: {
    flex: 1,
    minWidth: 0,
    padding: 14,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    overflow: "hidden"
  },
  kpiCardLabel: {
    color: palette.muted,
    fontSize: 10,
    fontWeight: "900",
    textTransform: "uppercase",
    letterSpacing: 0.6,
    marginBottom: 6
  },
  kpiCardValue: {
    color: palette.text,
    fontSize: 22,
    fontWeight: "900",
    lineHeight: 26
  },
  kpiCardSub: {
    color: palette.muted,
    fontSize: 11,
    marginTop: 5
  },
  kpiCardAccentBar: {
    position: "absolute",
    bottom: 0,
    left: 0,
    height: 3,
    width: "45%",
    backgroundColor: palette.accent,
    borderRadius: 9999
  },
  kpiTrendUp:   { color: "#22c55e" },
  kpiTrendDown: { color: "#ff6b6b" },

  /* Status / Condition Badges */
  badge: {
    flexDirection: "row",
    alignItems: "center",
    gap: 4,
    paddingHorizontal: 8,
    paddingVertical: 3,
    borderRadius: 9999,
    alignSelf: "flex-start"
  },
  badgeText: {
    fontSize: 10,
    fontWeight: "900",
    textTransform: "uppercase",
    letterSpacing: 0.5
  },
  badgeDot: {
    width: 5,
    height: 5,
    borderRadius: 9999
  },
  badgeAvailable:  { backgroundColor: "rgba(34,197,94,0.14)"  },
  badgeReserved:   { backgroundColor: "rgba(245,158,11,0.14)" },
  badgeSold:       { backgroundColor: "rgba(122,143,175,0.14)"},
  badgeDamaged:    { backgroundColor: "rgba(255,107,107,0.14)"},
  badgeNew:        { backgroundColor: "rgba(34,197,94,0.14)"  },
  badgeGood:       { backgroundColor: "rgba(0,201,167,0.12)"  },
  badgeFair:       { backgroundColor: "rgba(245,158,11,0.14)" },
  badgeForParts:   { backgroundColor: "rgba(255,107,107,0.12)"},
  badgeTextAvailable: { color: "#22c55e" },
  badgeTextReserved:  { color: "#f59e0b" },
  badgeTextSold:      { color: "#7a8faf" },
  badgeTextDamaged:   { color: "#ff6b6b" },
  badgeTextNew:       { color: "#22c55e" },
  badgeTextGood:      { color: palette.accent },
  badgeTextFair:      { color: "#f59e0b" },
  badgeTextForParts:  { color: "#ff6b6b" },

  /* Improved bottom tab bar */
  bottomTabBarV2: {
    height: 74,
    flexShrink: 0,
    flexDirection: "row",
    alignItems: "stretch",
    borderTopWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.sidebar,
    elevation: 24,
    shadowColor: "#000",
    shadowOpacity: 0.36,
    shadowRadius: 16,
    shadowOffset: { width: 0, height: -4 }
  },
  bottomTabButtonV2: {
    flex: 1,
    alignItems: "center",
    justifyContent: "flex-end",
    paddingBottom: 8,
    paddingTop: 6,
    gap: 4,
    position: "relative"
  },
  bottomTabIndicator: {
    position: "absolute",
    top: 0,
    left: "20%",
    right: "20%",
    height: 2,
    borderRadius: 9999,
    backgroundColor: palette.accent
  },
  bottomTabIconWrap: {
    width: 36,
    height: 30,
    alignItems: "center",
    justifyContent: "center"
  },

  /* Part List Item */
  partListItem: {
    flexDirection: "row",
    alignItems: "center",
    gap: 12,
    paddingHorizontal: 14,
    paddingVertical: 11,
    borderBottomWidth: 1,
    borderColor: palette.surface2,
    backgroundColor: palette.bg
  },
  partListItemThumb: {
    width: 60,
    height: 48,
    borderRadius: 2,
    backgroundColor: palette.surface2
  },
  partListItemInfo: {
    flex: 1,
    minWidth: 0
  },
  partListItemName: {
    color: palette.text,
    fontSize: 14,
    fontWeight: "800",
    marginBottom: 3
  },
  partListItemCode: {
    color: palette.muted,
    fontSize: 11,
    fontWeight: "800",
    letterSpacing: 0.3
  },
  partListItemRight: {
    alignItems: "flex-end",
    gap: 4,
    flexShrink: 0
  },
  partListItemPrice: {
    color: palette.accent,
    fontSize: 15,
    fontWeight: "900"
  },

  /* Part Detail Page */
  partDetailPhoto: {
    width: "100%",
    aspectRatio: 4 / 3,
    backgroundColor: palette.surface2,
    justifyContent: "center",
    alignItems: "center"
  },
  partDetailInfo: {
    padding: 16,
    gap: 12
  },
  partDetailName: {
    color: palette.text,
    fontSize: 22,
    fontWeight: "900",
    lineHeight: 28
  },
  partDetailCode: {
    color: palette.muted,
    fontSize: 12,
    fontWeight: "800",
    letterSpacing: 0.5
  },
  partDetailPrice: {
    color: palette.accent,
    fontSize: 26,
    fontWeight: "900"
  },
  partDetailSection: {
    paddingVertical: 14,
    borderTopWidth: 1,
    borderColor: palette.line
  },
  partDetailSectionTitle: {
    color: palette.muted,
    fontSize: 11,
    fontWeight: "900",
    textTransform: "uppercase",
    letterSpacing: 0.6,
    marginBottom: 8
  },

  /* Skeleton Loader */
  skeletonBase: {
    borderRadius: 2,
    backgroundColor: palette.surface2,
    overflow: "hidden"
  },
  skeletonRow: {
    flexDirection: "row",
    alignItems: "center",
    gap: 12,
    paddingHorizontal: 14,
    paddingVertical: 11,
    borderBottomWidth: 1,
    borderColor: palette.surface2
  },
  skeletonThumb: {
    width: 60,
    height: 48,
    borderRadius: 2
  },
  skeletonTextLg: {
    height: 14,
    borderRadius: 9999,
    marginBottom: 5
  },
  skeletonTextSm: {
    height: 11,
    borderRadius: 9999,
    width: "60%"
  },

  /* Empty State */
  emptyScreen: {
    flex: 1,
    alignItems: "center",
    justifyContent: "center",
    gap: 14,
    padding: 32
  },
  emptyScreenIcon: {
    width: 64,
    height: 64,
    borderRadius: 3,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: palette.surface2,
    borderWidth: 1,
    borderColor: palette.line
  },
  emptyScreenTitle: {
    color: palette.text,
    fontSize: 18,
    fontWeight: "900",
    textAlign: "center"
  },
  emptyScreenText: {
    color: palette.muted,
    fontSize: 14,
    textAlign: "center",
    lineHeight: 20,
    maxWidth: 280
  },

  /* Filter Chips */
  filterChipRail: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 8,
    paddingHorizontal: 14,
    paddingVertical: 8
  },
  filterChip: {
    flexDirection: "row",
    alignItems: "center",
    gap: 5,
    height: 30,
    paddingHorizontal: 10,
    borderRadius: 9999,
    borderWidth: 1,
    borderColor: `${palette.accent}60`,
    backgroundColor: `${palette.accent}18`
  },
  filterChipText: {
    color: palette.accent,
    fontSize: 12,
    fontWeight: "800"
  },
  filterChipRemove: {
    width: 16,
    height: 16,
    borderRadius: 9999,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: `${palette.accent}28`
  },
  filterChipRemoveText: {
    color: palette.accent,
    fontSize: 10,
    fontWeight: "900",
    lineHeight: 14
  },

  /* Improved WhatsApp Button */
  whatsappBtn: {
    flexDirection: "row",
    alignItems: "center",
    gap: 8,
    minHeight: 48,
    paddingHorizontal: 20,
    borderRadius: 2,
    backgroundColor: "#25d366",
    justifyContent: "center"
  },
  whatsappBtnText: {
    color: "#ffffff",
    fontSize: 14,
    fontWeight: "900"
  },

  /* Timeline */
  timeline: {
    gap: 0
  },
  timelineItem: {
    flexDirection: "row",
    gap: 12,
    paddingBottom: 18
  },
  timelineNode: {
    alignItems: "center",
    width: 20,
    marginTop: 2
  },
  timelineDot: {
    width: 10,
    height: 10,
    borderRadius: 9999,
    backgroundColor: palette.accent
  },
  timelineLine: {
    width: 2,
    flex: 1,
    backgroundColor: palette.line,
    marginTop: 4
  },
  timelineContent: {
    flex: 1,
    minWidth: 0
  },
  timelineTime: {
    color: palette.muted,
    fontSize: 11,
    marginBottom: 3
  },
  timelineAction: {
    color: palette.text,
    fontSize: 13,
    fontWeight: "800"
  },
  timelineDetail: {
    color: palette.muted,
    fontSize: 12,
    marginTop: 2,
    lineHeight: 17
  },

  /* Improved search bar */
  searchBarV2: {
    flexDirection: "row",
    alignItems: "center",
    gap: 8,
    minHeight: 46,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: `${palette.accent}48`,
    backgroundColor: palette.surface,
    paddingHorizontal: 12,
    shadowColor: "#000",
    shadowOpacity: 0.18,
    shadowRadius: 12,
    shadowOffset: { width: 0, height: 4 },
    elevation: 6
  },
  searchBarV2Input: {
    flex: 1,
    color: palette.text,
    fontSize: 15,
    paddingVertical: 0
  },
  searchBarV2Icon: {
    color: palette.accent,
    fontSize: 16
  },

  /* Improved top bar */
  topBarV2: {
    minHeight: 58,
    flexShrink: 0,
    flexDirection: "row",
    alignItems: "center",
    gap: 10,
    paddingHorizontal: 14,
    paddingVertical: 8,
    borderBottomWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.sidebar
  },
  topBarV2Title: {
    color: palette.text,
    fontSize: 19,
    fontWeight: "900"
  },
  topBarV2Sub: {
    color: palette.muted,
    fontSize: 11,
    marginTop: 1
  },

  /* Progress bar */
  progressBarWrap: {
    height: 5,
    borderRadius: 9999,
    backgroundColor: palette.surface2,
    overflow: "hidden"
  },
  progressBarFill: {
    height: "100%",
    borderRadius: 9999,
    backgroundColor: palette.accent
  },

  /* Action button strip */
  actionStrip: {
    flexDirection: "row",
    gap: 8,
    paddingHorizontal: 14,
    paddingVertical: 10,
    borderTopWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.sidebar
  },
  actionStripBtn: {
    flex: 1,
    minHeight: 44,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 2,
    backgroundColor: palette.surface2,
    borderWidth: 1,
    borderColor: palette.line
  },
  actionStripBtnPrimary: {
    backgroundColor: palette.accent,
    borderColor: palette.accent
  },
  actionStripBtnText: {
    color: palette.text,
    fontSize: 13,
    fontWeight: "900"
  },
  actionStripBtnTextPrimary: {
    color: palette.bg
  },

  /* ═══════════════════════════════════════════════════════════
     APEX MOBILE — Complete override block
     New palette: warm dark / orange accent / clean typography
     ═══════════════════════════════════════════════════════════ */

  app: { flex: 1, backgroundColor: palette.bg },
  appFrame: { flex: 1, flexDirection: "row", backgroundColor: palette.bg },
  contentPane: { flex: 1, backgroundColor: palette.bg, flexDirection: "column" },

  /* Top bar */
  topBar: {
    minHeight: 60,
    flexShrink: 0,
    flexDirection: "row",
    alignItems: "center",
    gap: 12,
    paddingHorizontal: 16,
    paddingVertical: 10,
    borderBottomWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    elevation: 3,
    shadowColor: "#000",
    shadowOpacity: 0.16,
    shadowRadius: 8,
    shadowOffset: { width: 0, height: 2 }
  },
  topBarTitle: {
    color: palette.text,
    fontSize: 18,
    fontWeight: "900",
    letterSpacing: -0.5
  },
  topBarSubtitle: {
    color: palette.muted,
    fontSize: 11,
    marginTop: 1
  },

  /* Sidebar */
  sidePanel: {
    width: 280,
    flexShrink: 0,
    zIndex: 30,
    borderRightWidth: 0,
    backgroundColor: palette.sidebar,
    borderRightColor: "transparent"
  },
  sideHeader: {
    minHeight: 68,
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 10,
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderColor: "rgba(255,255,255,0.08)"
  },
  sideBrandTitle: {
    color: "#f5f3f0",
    fontSize: 16,
    fontWeight: "900",
    letterSpacing: -0.3
  },
  sideBrandSubtitle: {
    color: "rgba(255,255,255,0.35)",
    fontSize: 11,
    marginTop: 2
  },
  sideBrandMark: {
    width: 38,
    height: 38,
    borderRadius: 2,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: palette.accent
  },
  navGroupTitle: {
    color: "rgba(255,255,255,0.28)",
    fontSize: 10,
    fontWeight: "700",
    textTransform: "uppercase",
    letterSpacing: 1
  },
  sideNavItem: {
    minHeight: 40,
    flexDirection: "row",
    alignItems: "center",
    gap: 10,
    borderRadius: 2,
    borderWidth: 0,
    backgroundColor: "transparent",
    paddingHorizontal: 10
  },
  sideNavItemActive: {
    backgroundColor: palette.accent,
    borderColor: "transparent"
  },
  sideNavText: {
    flex: 1,
    color: "rgba(255,255,255,0.5)",
    fontSize: 13,
    fontWeight: "600"
  },
  sideNavTextActive: {
    color: "#fff",
    fontWeight: "700"
  },
  navActiveMark: { display: "none" },
  navActiveMarkOn: { display: "none" },
  sideUserPanel: {
    borderTopWidth: 1,
    borderColor: "rgba(255,255,255,0.08)",
    padding: 12,
    gap: 10
  },
  sideAvatar: {
    width: 36,
    height: 36,
    borderRadius: 18,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: palette.accent
  },
  sideUserName: {
    color: "#f5f3f0",
    fontSize: 13,
    fontWeight: "800"
  },
  sideUserRole: {
    color: "rgba(255,255,255,0.38)",
    fontSize: 11,
    marginTop: 2
  },
  sideLogoutButton: {
    minHeight: 36,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(255,255,255,0.12)",
    backgroundColor: "rgba(255,255,255,0.06)"
  },
  sideLogoutText: {
    color: "rgba(255,255,255,0.55)",
    fontSize: 12,
    fontWeight: "700"
  },
  sideCloseButton: {
    minHeight: 32,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: "rgba(255,255,255,0.1)",
    backgroundColor: "rgba(255,255,255,0.06)",
    alignItems: "center",
    justifyContent: "center",
    paddingHorizontal: 10
  },
  sideCloseText: { color: "rgba(255,255,255,0.5)", fontSize: 11, fontWeight: "700" },

  /* Bottom tabs — orange active state */
  bottomTabBar: {
    height: 82,
    flexShrink: 0,
    flexDirection: "row",
    alignItems: "stretch",
    paddingHorizontal: 6,
    paddingTop: 6,
    paddingBottom: 10,
    borderTopWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    elevation: 20,
    shadowColor: "#000",
    shadowOpacity: 0.3,
    shadowRadius: 16,
    shadowOffset: { width: 0, height: -4 }
  },
  bottomTabButton: {
    flex: 1,
    alignItems: "center",
    justifyContent: "center",
    gap: 3,
    borderRadius: 2,
    backgroundColor: "transparent",
    paddingHorizontal: 4,
    paddingVertical: 6,
    marginHorizontal: 2
  },
  bottomTabButtonActive: {
    backgroundColor: `${palette.accent}18`
  },
  bottomTabMark: {
    width: 44,
    height: 32,
    alignItems: "center",
    justifyContent: "center"
  },
  bottomTabMarkActive: { backgroundColor: "transparent" },
  bottomTabLabel: {
    color: palette.muted,
    fontSize: 10,
    fontWeight: "600",
    textAlign: "center"
  },
  bottomTabLabelActive: {
    color: palette.accent,
    fontWeight: "800"
  },
  bottomTabDescription: { display: "none" },
  bottomTabDescriptionActive: { display: "none" },

  /* Screen content */
  screenTitle: {
    color: palette.text,
    fontSize: 24,
    fontWeight: "900",
    letterSpacing: -0.8
  },
  screenContent: {
    padding: 16,
    gap: 14,
    paddingBottom: 32
  },

  /* Cards & panels */
  panel: {
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    padding: 16,
    gap: 12,
    elevation: 2,
    shadowColor: "#000",
    shadowOpacity: 0.12,
    shadowRadius: 8,
    shadowOffset: { width: 0, height: 3 }
  },
  panelTitle: {
    color: palette.text,
    fontSize: 16,
    fontWeight: "900",
    letterSpacing: -0.4
  },

  /* Metric tiles */
  metricGrid: { flexDirection: "row", flexWrap: "wrap", gap: 10 },
  metricTile: {
    width: "48%",
    minHeight: 88,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    padding: 14,
    justifyContent: "space-between",
    elevation: 2,
    shadowColor: "#000",
    shadowOpacity: 0.1,
    shadowRadius: 8,
    shadowOffset: { width: 0, height: 3 }
  },
  metricLabel: {
    color: palette.soft,
    fontSize: 10,
    fontWeight: "800",
    textTransform: "uppercase",
    letterSpacing: 0.8
  },
  metricValue: {
    color: palette.text,
    fontSize: 24,
    fontWeight: "900",
    letterSpacing: -0.8,
    marginTop: 6
  },
  metricAction: {
    color: palette.accent,
    fontSize: 10,
    fontWeight: "800",
    marginTop: 6,
    textTransform: "uppercase",
    letterSpacing: 0.5
  },

  /* Dashboard action cards */
  dashboardActionButton: {
    width: "48%",
    minHeight: 116,
    justifyContent: "space-between",
    gap: 8,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    padding: 14,
    elevation: 2,
    shadowColor: "#000",
    shadowOpacity: 0.1,
    shadowRadius: 8,
    shadowOffset: { width: 0, height: 3 }
  },
  dashboardActionButtonFeatured: {
    borderColor: palette.accent,
    elevation: 4,
    shadowColor: palette.accent,
    shadowOpacity: 0.22
  },
  dashboardActionBadge: {
    alignSelf: "flex-start",
    color: palette.accent,
    fontSize: 10,
    fontWeight: "800",
    textTransform: "uppercase",
    letterSpacing: 0.8
  },
  dashboardActionTitle: {
    color: palette.text,
    fontSize: 15,
    fontWeight: "900",
    letterSpacing: -0.3
  },
  dashboardActionSubtitle: {
    color: palette.muted,
    fontSize: 12,
    lineHeight: 17
  },

  /* Inputs & buttons */
  input: {
    minHeight: 46,
    borderRadius: 2,
    borderWidth: 1.5,
    borderColor: palette.line,
    backgroundColor: palette.input,
    color: palette.text,
    paddingHorizontal: 14,
    fontSize: 14
  },
  fieldLabel: {
    color: palette.muted,
    fontSize: 11,
    fontWeight: "700",
    textTransform: "uppercase",
    letterSpacing: 0.6
  },
  primaryButton: {
    minHeight: 48,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 2,
    backgroundColor: palette.accent,
    paddingHorizontal: 20,
    elevation: 4,
    shadowColor: palette.accent,
    shadowOpacity: 0.3,
    shadowRadius: 12,
    shadowOffset: { width: 0, height: 4 }
  },
  primaryButtonText: {
    color: "#fff",
    fontWeight: "900",
    fontSize: 14,
    letterSpacing: 0.2
  },
  secondaryButton: {
    minHeight: 42,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 2,
    borderWidth: 1.5,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    paddingHorizontal: 14
  },
  secondaryButtonText: {
    color: palette.text,
    fontWeight: "700",
    fontSize: 13
  },

  /* List rows */
  listRow: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 12,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderColor: palette.line
  },
  dashboardActionRow: {
    minHeight: 56,
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 12,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    paddingHorizontal: 14,
    paddingVertical: 10,
    elevation: 1,
    shadowColor: "#000",
    shadowOpacity: 0.06,
    shadowRadius: 6,
    shadowOffset: { width: 0, height: 2 }
  },

  /* Login */
  loginPanel: {
    padding: 22,
    borderRadius: 3,
    borderWidth: 1.5,
    borderTopColor: "rgba(255,255,255,0.18)",
    borderLeftColor: "rgba(255,255,255,0.10)",
    borderRightColor: "rgba(255,255,255,0.08)",
    borderBottomColor: "rgba(255,255,255,0.06)",
    backgroundColor: "rgba(10,9,8,0.92)",
    gap: 14,
    overflow: "hidden",
    elevation: 12,
    shadowColor: "#000",
    shadowOpacity: 0.45,
    shadowRadius: 30,
    shadowOffset: { width: 0, height: 14 }
  },
  loginTitle: {
    color: "#f5f3f0",
    fontSize: 38,
    lineHeight: 40,
    fontWeight: "900",
    letterSpacing: -1.2
  },
  loginSubtitle: {
    color: "#8a8580",
    fontSize: 14,
    lineHeight: 20
  },
  loginEyebrow: {
    color: palette.accent,
    fontSize: 11,
    fontWeight: "800",
    textTransform: "uppercase",
    letterSpacing: 1.2
  },
  loginPanelTitle: {
    color: "#f5f3f0",
    fontSize: 22,
    fontWeight: "900",
    marginTop: 2,
    letterSpacing: -0.5
  },
  loginPanelKicker: {
    color: palette.accent,
    fontSize: 11,
    fontWeight: "800",
    textTransform: "uppercase",
    letterSpacing: 0.8
  },

  /* Smart search */
  smartSearchInputRow: {
    minHeight: 54,
    flexDirection: "row",
    alignItems: "center",
    gap: 10,
    borderRadius: 2,
    borderWidth: 1.5,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    paddingHorizontal: 12,
    elevation: 3,
    shadowColor: "#000",
    shadowOpacity: 0.14,
    shadowRadius: 10,
    shadowOffset: { width: 0, height: 3 }
  },
  smartSearchMark: {
    width: 34,
    height: 34,
    borderRadius: 2,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: palette.accent
  },
  smartSearchInput: {
    flex: 1,
    minWidth: 0,
    minHeight: 42,
    color: palette.text,
    fontSize: 15,
    paddingVertical: 0
  },
  smartSearchResult: {
    minHeight: 52,
    borderRadius: 2,
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 10,
    backgroundColor: palette.surface2,
    paddingHorizontal: 12,
    paddingVertical: 10,
    borderWidth: 1,
    borderColor: palette.line
  },

  /* Heatmap */
  heatmapTile: {
    minHeight: 170,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    padding: 14,
    gap: 10,
    elevation: 2,
    shadowColor: "#000",
    shadowOpacity: 0.1,
    shadowRadius: 8,
    shadowOffset: { width: 0, height: 3 }
  },
  heatmapTileGreen:  { borderLeftWidth: 4, borderLeftColor: "#16a34a" },
  heatmapTileYellow: { borderLeftWidth: 4, borderLeftColor: "#d97706" },
  heatmapTileRed:    { borderLeftWidth: 4, borderLeftColor: "#dc2626" },

  /* Profit/loss hero */
  profitLossHero: {
    minHeight: 120,
    justifyContent: "center",
    gap: 6,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.text,
    padding: 16
  },
  profitLossHeroLabel: {
    color: "rgba(15,14,12,0.45)",
    fontSize: 10,
    fontWeight: "800",
    textTransform: "uppercase",
    letterSpacing: 0.8
  },
  profitLossHeroValue: {
    fontSize: 34,
    lineHeight: 38,
    fontWeight: "900",
    letterSpacing: -1,
    color: palette.bg
  },
  profitLossHeroMeta: {
    color: "rgba(15,14,12,0.55)",
    fontSize: 12,
    fontWeight: "700"
  },
  profitLossRow: {
    width: "48%",
    minHeight: 64,
    justifyContent: "space-between",
    gap: 6,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    padding: 12
  },
  profitLossRowLabel: {
    color: palette.soft,
    fontSize: 10,
    fontWeight: "700",
    textTransform: "uppercase",
    letterSpacing: 0.6
  },
  profitLossRowValue: {
    fontSize: 16,
    fontWeight: "900",
    letterSpacing: -0.4,
    color: palette.text
  },

  /* Billing & subscription */
  billingPlanCard: {
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    padding: 14,
    gap: 8
  },
  billingPlanCardActive: {
    borderColor: palette.accent
  },
  billingPlanHeader: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 8
  },
  billingPlanName: {
    color: palette.text,
    fontSize: 15,
    fontWeight: "900"
  },
  billingPlanDescription: {
    color: palette.muted,
    fontSize: 12,
    lineHeight: 17
  },
  billingPlanPrice: {
    color: palette.accent,
    fontSize: 16,
    fontWeight: "900"
  },
  billingBadge: {
    alignSelf: "flex-start",
    borderRadius: 2,
    paddingHorizontal: 8,
    paddingVertical: 3,
    fontSize: 10,
    fontWeight: "900",
    textTransform: "uppercase",
    letterSpacing: 0.6,
    overflow: "hidden",
    color: "#fff"
  },
  billingBadgeSuccess: { backgroundColor: "#16a34a" },
  billingBadgeWarning: { backgroundColor: "#d97706" },
  billingBadgeDanger: { backgroundColor: "#dc2626" },
  billingBadgeAccent: { backgroundColor: palette.accent },
  billingBadgeMuted: { backgroundColor: palette.soft },

  /* Locked feature modal */
  lockModalOverlay: {
    flex: 1,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: "rgba(0,0,0,0.55)",
    padding: 20
  },
  lockModalCard: {
    width: "100%",
    maxWidth: 380,
    borderRadius: 3,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    padding: 18,
    gap: 10
  },
  lockModalTitle: {
    color: palette.text,
    fontSize: 17,
    fontWeight: "900"
  },
  lockModalMessage: {
    color: palette.text,
    fontSize: 13,
    lineHeight: 19
  },
  lockModalMeta: {
    color: palette.muted,
    fontSize: 12,
    fontWeight: "700"
  },
  lockModalActions: {
    flexDirection: "row",
    gap: 8,
    marginTop: 8
  },

  /* ---------------------------------------------------------------- */
  /* New shared UI component library (src/components/ui/*)            */
  /* ---------------------------------------------------------------- */

  uiCard: {
    borderRadius: 3,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    padding: 14,
    gap: 8,
    shadowColor: "#000000",
    shadowOpacity: 0.16,
    shadowRadius: 10,
    shadowOffset: { width: 0, height: 4 },
    elevation: 4
  },
  uiCardPressed: {
    borderColor: palette.accent,
    opacity: 0.92
  },
  uiCardCompact: {
    padding: 10,
    gap: 6
  },
  uiCardRow: {
    flexDirection: "row",
    gap: 12,
    alignItems: "center"
  },
  uiCardHeaderRow: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 8
  },

  uiSectionHeader: {
    flexDirection: "row",
    alignItems: "flex-end",
    justifyContent: "space-between",
    marginTop: 18,
    marginBottom: 8,
    gap: 10
  },
  uiSectionHeaderTextWrap: {
    flex: 1,
    minWidth: 0,
    gap: 2
  },
  uiSectionHeaderTitle: {
    color: palette.text,
    fontSize: 15,
    fontWeight: "900"
  },
  uiSectionHeaderSubtitle: {
    color: palette.muted,
    fontSize: 12,
    fontWeight: "600"
  },
  uiSectionHeaderAction: {
    minHeight: 32,
    paddingHorizontal: 12,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.accent,
    alignItems: "center",
    justifyContent: "center"
  },
  uiSectionHeaderActionText: {
    color: palette.accent,
    fontSize: 12,
    fontWeight: "800"
  },

  uiStatusPill: {
    flexDirection: "row",
    alignItems: "center",
    gap: 5,
    alignSelf: "flex-start",
    borderRadius: 999,
    borderWidth: 1,
    paddingHorizontal: 9,
    paddingVertical: 4
  },
  uiStatusPillDot: {
    width: 6,
    height: 6,
    borderRadius: 3
  },
  uiStatusPillText: {
    fontSize: 11,
    fontWeight: "800"
  },
  uiStatusPillNeutral: {
    borderColor: palette.line,
    backgroundColor: palette.surface2
  },
  uiStatusPillNeutralText: { color: palette.muted },
  uiStatusPillNeutralDot: { backgroundColor: palette.muted },
  uiStatusPillAccent: {
    borderColor: palette.accent,
    backgroundColor: "rgba(255,255,255,0.04)"
  },
  uiStatusPillAccentText: { color: palette.accent },
  uiStatusPillAccentDot: { backgroundColor: palette.accent },
  uiStatusPillGood: {
    borderColor: "#66bb6a",
    backgroundColor: "rgba(102,187,106,0.14)"
  },
  uiStatusPillGoodText: { color: "#66bb6a" },
  uiStatusPillGoodDot: { backgroundColor: "#66bb6a" },
  uiStatusPillWarn: {
    borderColor: "#ffb74d",
    backgroundColor: "rgba(255,183,77,0.14)"
  },
  uiStatusPillWarnText: { color: "#ffb74d" },
  uiStatusPillWarnDot: { backgroundColor: "#ffb74d" },
  uiStatusPillBad: {
    borderColor: "#e57373",
    backgroundColor: "rgba(229,115,115,0.16)"
  },
  uiStatusPillBadText: { color: "#e57373" },
  uiStatusPillBadDot: { backgroundColor: "#e57373" },

  uiSearchBarRow: {
    minHeight: 46,
    flexDirection: "row",
    alignItems: "center",
    gap: 8,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input,
    paddingHorizontal: 12
  },
  uiSearchBarIcon: {
    color: palette.muted,
    fontSize: 15,
    fontWeight: "900"
  },
  uiSearchBarInput: {
    flex: 1,
    minWidth: 0,
    minHeight: 40,
    color: palette.text,
    fontSize: 14,
    paddingVertical: 0
  },
  uiSearchBarClear: {
    width: 22,
    height: 22,
    borderRadius: 11,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: palette.surface2
  },
  uiSearchBarClearText: {
    color: palette.muted,
    fontSize: 12,
    fontWeight: "900"
  },

  uiFilterChipRail: {
    gap: 8,
    paddingVertical: 2,
    paddingRight: 18
  },
  uiFilterChip: {
    minHeight: 36,
    borderRadius: 999,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    paddingHorizontal: 14,
    alignItems: "center",
    justifyContent: "center",
    flexDirection: "row",
    gap: 6
  },
  uiFilterChipActive: {
    borderColor: palette.accent,
    backgroundColor: palette.accent
  },
  uiFilterChipText: {
    color: palette.muted,
    fontSize: 12,
    fontWeight: "800"
  },
  uiFilterChipTextActive: {
    color: "#ffffff"
  },
  uiFilterChipCount: {
    color: palette.muted,
    fontSize: 11,
    fontWeight: "800"
  },
  uiFilterChipCountActive: {
    color: "rgba(255,255,255,0.85)"
  },
  uiFilterScrollWrap: {
    position: "relative"
  },
  uiFilterScrollFadeRight: {
    position: "absolute",
    right: 0,
    top: 0,
    bottom: 0,
    width: 22
  },

  uiEmptyState: {
    alignItems: "center",
    justifyContent: "center",
    gap: 6,
    paddingVertical: 30,
    paddingHorizontal: 18
  },
  uiEmptyStateIcon: {
    width: 52,
    height: 52,
    borderRadius: 26,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: palette.surface2,
    marginBottom: 4
  },
  uiEmptyStateIconText: {
    fontSize: 22
  },
  uiEmptyStateTitle: {
    color: palette.text,
    fontSize: 14,
    fontWeight: "900",
    textAlign: "center"
  },
  uiEmptyStateText: {
    color: palette.muted,
    fontSize: 12,
    fontWeight: "600",
    textAlign: "center",
    lineHeight: 17
  },
  uiEmptyStateAction: {
    marginTop: 8,
    minHeight: 38,
    paddingHorizontal: 16,
    borderRadius: 2,
    borderWidth: 1,
    borderColor: palette.accent,
    alignItems: "center",
    justifyContent: "center"
  },
  uiEmptyStateActionText: {
    color: palette.accent,
    fontSize: 12,
    fontWeight: "800"
  },

  uiSkeletonBlock: {
    overflow: "hidden",
    borderRadius: 2,
    backgroundColor: palette.surface2
  },
  uiSkeletonShimmer: {
    flex: 1,
    backgroundColor: "rgba(255,255,255,0.10)"
  },
  uiSkeletonCard: {
    borderRadius: 3,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    padding: 14,
    gap: 10
  },
  uiSkeletonRow: {
    flexDirection: "row",
    gap: 10,
    alignItems: "center"
  },

  uiStickyBar: {
    position: "absolute",
    left: 0,
    right: 0,
    bottom: 0,
    flexDirection: "row",
    gap: 10,
    paddingHorizontal: 14,
    paddingTop: 10,
    borderTopWidth: 1,
    borderTopColor: palette.line,
    backgroundColor: palette.surface,
    shadowColor: "#000000",
    shadowOpacity: 0.22,
    shadowRadius: 14,
    shadowOffset: { width: 0, height: -4 },
    elevation: 12
  },
  uiStickyBarButton: {
    flex: 1,
    minHeight: 46,
    borderRadius: 2,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: palette.accent
  },
  uiStickyBarButtonSecondary: {
    backgroundColor: palette.surface2,
    borderWidth: 1,
    borderColor: palette.line
  },
  uiStickyBarButtonDanger: {
    backgroundColor: "#e57373"
  },
  uiStickyBarButtonText: {
    color: "#ffffff",
    fontSize: 13,
    fontWeight: "900"
  },
  uiStickyBarButtonSecondaryText: {
    color: palette.text
  },
  uiStickyBarButtonDisabled: {
    opacity: 0.5
  },
  uiStickyBarSpacer: {
    height: 64
  },

  uiSheetBackdrop: {
    position: "absolute",
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: "rgba(0,0,0,0.5)",
    justifyContent: "flex-end"
  },
  uiSheetCard: {
    maxHeight: "88%",
    borderTopLeftRadius: 18,
    borderTopRightRadius: 18,
    borderWidth: 1,
    borderColor: palette.line,
    borderBottomWidth: 0,
    backgroundColor: palette.surface,
    paddingTop: 8,
    shadowColor: "#000000",
    shadowOpacity: 0.3,
    shadowRadius: 20,
    shadowOffset: { width: 0, height: -6 },
    elevation: 20
  },
  uiSheetHandleRow: {
    alignItems: "center",
    paddingVertical: 8
  },
  uiSheetHandle: {
    width: 40,
    height: 4,
    borderRadius: 2,
    backgroundColor: palette.line
  },
  uiSheetHeaderRow: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    paddingHorizontal: 16,
    paddingBottom: 10,
    borderBottomWidth: 1,
    borderBottomColor: palette.line
  },
  uiSheetTitle: {
    flex: 1,
    minWidth: 0,
    color: palette.text,
    fontSize: 16,
    fontWeight: "900"
  },
  uiSheetCloseButton: {
    width: 30,
    height: 30,
    borderRadius: 15,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: palette.surface2
  },
  uiSheetCloseText: {
    color: palette.muted,
    fontSize: 14,
    fontWeight: "900"
  },
  uiSheetBody: {
    paddingHorizontal: 16,
    paddingTop: 12,
    paddingBottom: 24,
    gap: 10
  },

  uiGalleryRoot: {
    width: "100%",
    backgroundColor: palette.input,
    borderRadius: 2,
    overflow: "hidden"
  },
  uiGalleryImageWrap: {
    width: "100%",
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: palette.input,
    overflow: "hidden"
  },
  uiGalleryImage: {
    width: "100%",
    height: "100%"
  },
  uiGalleryCounterBadge: {
    position: "absolute",
    top: 10,
    right: 10,
    borderRadius: 999,
    backgroundColor: "rgba(0,0,0,0.55)",
    paddingHorizontal: 9,
    paddingVertical: 4
  },
  uiGalleryCounterText: {
    color: "#ffffff",
    fontSize: 11,
    fontWeight: "800"
  },
  uiGalleryArrow: {
    position: "absolute",
    top: "50%",
    width: 34,
    height: 34,
    marginTop: -17,
    borderRadius: 17,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: "rgba(0,0,0,0.45)"
  },
  uiGalleryArrowLeft: { left: 8 },
  uiGalleryArrowRight: { right: 8 },
  uiGalleryArrowText: {
    color: "#ffffff",
    fontSize: 16,
    fontWeight: "900"
  },
  uiGalleryThumbRail: {
    gap: 8,
    paddingVertical: 8,
    paddingHorizontal: 4
  },
  uiGalleryThumb: {
    width: 52,
    height: 52,
    borderRadius: 2,
    overflow: "hidden",
    borderWidth: 2,
    borderColor: "transparent",
    backgroundColor: palette.input
  },
  uiGalleryThumbActive: {
    borderColor: palette.accent
  },
  uiGalleryThumbImage: {
    width: "100%",
    height: "100%"
  },
  uiGalleryEmpty: {
    alignItems: "center",
    justifyContent: "center"
  },
  uiGalleryEmptyText: {
    color: palette.muted,
    fontSize: 12,
    fontWeight: "700"
  },
  uiGalleryZoomHint: {
    position: "absolute",
    bottom: 10,
    left: 10,
    borderRadius: 999,
    backgroundColor: "rgba(0,0,0,0.45)",
    paddingHorizontal: 8,
    paddingVertical: 3
  },
  uiGalleryZoomHintText: {
    color: "#ffffff",
    fontSize: 10,
    fontWeight: "700"
  },

  uiPartCard: {
    flexDirection: "row",
    gap: 12,
    borderRadius: 3,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    padding: 12
  },
  uiPartCardPressed: {
    borderColor: palette.accent
  },
  uiPartCardThumb: {
    width: 64,
    height: 64,
    borderRadius: 2,
    overflow: "hidden",
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: palette.input
  },
  uiPartCardThumbImage: {
    width: "100%",
    height: "100%"
  },
  uiPartCardThumbText: {
    color: palette.muted,
    fontSize: 16,
    fontWeight: "900"
  },
  uiPartCardBody: {
    flex: 1,
    minWidth: 0,
    gap: 4
  },
  uiPartCardTitle: {
    color: palette.text,
    fontSize: 14,
    fontWeight: "900"
  },
  uiPartCardSubtitle: {
    color: palette.muted,
    fontSize: 12,
    fontWeight: "600"
  },
  uiPartCardBadgeRow: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 6,
    marginTop: 2
  },
  uiPartCardSide: {
    alignItems: "flex-end",
    justifyContent: "space-between",
    gap: 6
  },
  uiPartCardPrice: {
    color: palette.text,
    fontSize: 15,
    fontWeight: "900"
  },
  uiPartCardActionsRow: {
    flexDirection: "row",
    gap: 6
  },
  uiPartCardIconButton: {
    width: 30,
    height: 30,
    borderRadius: 2,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: palette.surface2,
    borderWidth: 1,
    borderColor: palette.line
  },
  uiPartCardIconButtonText: {
    fontSize: 13
  },
  uiPartGrid: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 10
  },
  uiPartCardGridItem: {
    flexDirection: "column",
    width: "48%"
  },
  uiPartCardGridThumb: {
    width: "100%",
    height: 110,
    borderRadius: 2,
    marginBottom: 8
  },

  uiVehicleCard: {
    borderRadius: 3,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    overflow: "hidden"
  },
  uiVehicleCardImage: {
    width: "100%",
    height: 150,
    backgroundColor: palette.input
  },
  uiVehicleCardImagePlaceholder: {
    alignItems: "center",
    justifyContent: "center"
  },
  uiVehicleCardBody: {
    padding: 12,
    gap: 6
  },
  uiVehicleCardTitleRow: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 8
  },
  uiVehicleCardTitle: {
    flex: 1,
    minWidth: 0,
    color: palette.text,
    fontSize: 15,
    fontWeight: "900"
  },
  uiVehicleCardPrice: {
    color: palette.accent,
    fontSize: 15,
    fontWeight: "900"
  },
  uiVehicleCardMetaRow: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 10
  },
  uiVehicleCardMetaItem: {
    color: palette.muted,
    fontSize: 12,
    fontWeight: "700"
  },
  uiVehicleCardFooterRow: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    marginTop: 4
  },

  phoneHeaderBar: {
    minHeight: 46,
    flexDirection: "row",
    alignItems: "center",
    gap: 12,
    paddingHorizontal: 14,
    paddingVertical: 8,
    borderBottomWidth: 1,
    borderBottomColor: palette.line,
    backgroundColor: palette.bg
  },
  phoneHeaderMenuButton: {
    width: 38,
    height: 38,
    borderRadius: 2,
    alignItems: "center",
    justifyContent: "center",
    gap: 4,
    backgroundColor: palette.surface2
  },
  phoneHeaderMenuLine: {
    width: 16,
    height: 2,
    borderRadius: 2,
    backgroundColor: palette.text
  },
  phoneHeaderTitle: {
    flex: 1,
    minWidth: 0,
    color: palette.text,
    fontSize: 15,
    fontWeight: "900"
  }
});
}

module.exports = { createPalette, createStyles };
