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
    minHeight: 52,
    flexDirection: "row",
    alignItems: "center",
    gap: 9,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: palette.accent,
    backgroundColor: palette.surface,
    paddingHorizontal: 10,
    shadowColor: "#000000",
    shadowOpacity: 0.22,
    shadowRadius: 16,
    shadowOffset: { width: 0, height: 8 },
    elevation: 10
  },
  smartSearchMark: {
    width: 32,
    height: 32,
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    minHeight: 62,
    flexShrink: 0,
    flexDirection: "row",
    alignItems: "center",
    gap: 12,
    paddingHorizontal: 14,
    paddingVertical: 10,
    borderBottomWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.bg
  },
  menuButton: {
    minWidth: 68,
    minHeight: 40,
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 4,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    backgroundColor: "#050609"
  },
  loginBackdrop: {
    flex: 1,
    backgroundColor: "#050609"
  },
  loginBackdropImage: {
    opacity: 0.98
  },
  loginShade: {
    flex: 1,
    backgroundColor: "rgba(5,6,9,0.10)"
  },
  loginWarmGlow: {
    position: "absolute",
    top: -60,
    right: 28,
    width: 32,
    height: 460,
    borderRadius: 8,
    backgroundColor: "rgba(255,59,31,0.20)",
    transform: [{ rotate: "14deg" }]
  },
  loginCoolGlow: {
    position: "absolute",
    top: 10,
    left: -28,
    width: 24,
    height: 340,
    borderRadius: 8,
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
    borderRadius: 5,
    backgroundColor: "rgba(255,255,255,0.18)"
  },
  loginFuelStripHot: {
    flex: 3,
    backgroundColor: "#ff3b1f"
  },
  loginFuelStripWarm: {
    flex: 2,
    backgroundColor: "#ffb000"
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
    padding: 18,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: "rgba(255,255,255,0.18)",
    backgroundColor: "rgba(9,10,14,0.86)",
    gap: 13,
    overflow: "hidden"
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
    color: "#ffb000",
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
    borderRadius: 8,
    borderWidth: 1,
    borderColor: "rgba(255,176,0,0.44)",
    backgroundColor: "rgba(255,176,0,0.13)"
  },
  loginPanelBadgeText: {
    color: "#ffcf5b",
    fontSize: 11,
    fontWeight: "900"
  },
  loginEyebrow: {
    color: "#ffb000",
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 12,
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
    borderRadius: 12,
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
    borderRadius: 8,
    borderWidth: 1,
    borderColor: "rgba(255,255,255,0.30)",
    backgroundColor: "#ff3b1f",
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    height: 92,
    flexShrink: 0,
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 6,
    paddingHorizontal: 8,
    paddingTop: 8,
    paddingBottom: 10,
    borderTopWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.sidebar,
    elevation: 20,
    shadowColor: "#000000",
    shadowOpacity: 0.32,
    shadowRadius: 14,
    shadowOffset: { width: 0, height: -4 }
  },
  bottomTabButton: {
    flex: 1,
    minWidth: 0,
    minHeight: 70,
    alignItems: "center",
    justifyContent: "center",
    gap: 2,
    borderRadius: 8,
    backgroundColor: "transparent",
    paddingHorizontal: 4
  },
  bottomTabButtonActive: {
    backgroundColor: "transparent"
  },
  bottomTabMark: {
    width: 42,
    height: 34,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: "transparent"
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
    color: palette.text
  },
  bottomTabLabel: {
    maxWidth: "100%",
    color: palette.muted,
    fontSize: 10,
    fontWeight: "800",
    textAlign: "center"
  },
  bottomTabLabelActive: {
    color: palette.accent
  },
  bottomTabDescription: {
    maxWidth: "100%",
    color: palette.soft,
    fontSize: 8,
    fontWeight: "800",
    textAlign: "center"
  },
  bottomTabDescriptionActive: {
    color: palette.text
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
    borderRadius: 3,
    transform: [{ rotate: "-35deg" }]
  },
  iconWrenchGrip: {
    position: "absolute",
    left: 17,
    top: 13,
    width: 6,
    height: 4,
    borderRadius: 4,
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
    borderRadius: 8,
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
    borderRadius: 4,
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
    fontSize: 28,
    fontWeight: "900"
  },
  headerAction: {
    minWidth: 86,
    minHeight: 38,
    alignItems: "center",
    justifyContent: "center",
    paddingHorizontal: 12,
    borderRadius: 8,
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
    minHeight: 44,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input,
    color: palette.text,
    paddingHorizontal: 12
  },
  statusText: {
    color: palette.muted,
    fontSize: 12
  },
  primaryButton: {
    minHeight: 46,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 8,
    backgroundColor: palette.accent,
    paddingHorizontal: 14
  },
  compactButton: {
    minHeight: 48
  },
  primaryButtonText: {
    color: palette.text,
    fontWeight: "900"
  },
  secondaryButton: {
    minHeight: 38,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 8,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    paddingHorizontal: 12
  },
  secondaryButtonText: {
    color: palette.text,
    fontWeight: "800",
    fontSize: 12
  },
  disabledButton: {
    opacity: 0.55
  },
  metricGrid: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 10
  },
  metricTile: {
    width: "48%",
    minHeight: 82,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    padding: 12,
    justifyContent: "space-between"
  },
  metricLabel: {
    color: palette.muted,
    fontSize: 12,
    fontWeight: "700"
  },
  metricValue: {
    color: palette.text,
    fontSize: 17,
    fontWeight: "900"
  },
  metricAction: {
    color: palette.accent,
    fontSize: 11,
    fontWeight: "900",
    marginTop: 8
  },
  dashboardActionGrid: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 10
  },
  dashboardActionButton: {
    width: "48%",
    minHeight: 112,
    justifyContent: "space-between",
    gap: 7,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface2,
    padding: 12
  },
  dashboardActionButtonFeatured: {
    borderColor: palette.accent,
    backgroundColor: palette.surface
  },
  dashboardActionBadge: {
    alignSelf: "flex-start",
    color: palette.accent,
    fontSize: 10,
    fontWeight: "900",
    textTransform: "uppercase"
  },
  dashboardActionTitle: {
    color: palette.text,
    fontSize: 16,
    fontWeight: "900"
  },
  dashboardActionSubtitle: {
    color: palette.muted,
    fontSize: 12,
    lineHeight: 17
  },
  panel: {
    borderRadius: 8,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    padding: 14,
    gap: 10
  },
  panelTitle: {
    color: palette.text,
    fontSize: 18,
    fontWeight: "900"
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
    borderRadius: 8,
    borderWidth: 1,
    borderColor: "rgba(255,255,255,0.06)",
    backgroundColor: palette.surface2,
    paddingHorizontal: 10,
    paddingVertical: 9
  },
  heatmapGrid: {
    gap: 10
  },
  heatmapTile: {
    minHeight: 188,
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input
  },
  screenListFrameLarge: {
    height: 460,
    overflow: "hidden",
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input
  },
  mechanicPartCard: {
    minHeight: 72,
    flexDirection: "row",
    alignItems: "center",
    gap: 12,
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input
  },
  deadStockQueueItem: {
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input
  },
  compatEmptyGraph: {
    minHeight: 220,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 13,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
    backgroundColor: palette.surface2
  },
  prepProgressFill: {
    height: "100%",
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
    backgroundColor: palette.input
  },
  prepTaskRow: {
    minHeight: 54,
    flexDirection: "row",
    alignItems: "center",
    gap: 10,
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
  usedCarThumbRail: {
    gap: 8,
    paddingVertical: 2
  },
  usedCarThumb: {
    width: 66,
    height: 50,
    overflow: "hidden",
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.input
  },
  usedCarInventoryRow: {
    minHeight: 68,
    borderRadius: 8,
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
    borderRadius: 8,
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
  usedCarPartsPanel: {
    gap: 9
  },
  usedCarPartsListFrame: {
    height: 210,
    overflow: "hidden",
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
    borderWidth: 1,
    borderColor: palette.line,
    backgroundColor: palette.surface,
    padding: 10,
    justifyContent: "center",
    gap: 4
  },
  whatsAppCampaignAsset: {
    minHeight: 68,
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
    borderRadius: 8,
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
  }
});
}

module.exports = { createPalette, createStyles };
