import { useState, useEffect } from 'react';
import {
  Settings,
  Globe,
  Save,
  Check,
  Calendar,
  Clock,
  DollarSign,
  Languages,
  Bug,
  FileText,
  RefreshCw,
  User,
  Shield,
  Bell,
  Palette,
  Database,
  BookOpen,
  Key,
  ChevronRight,
} from 'lucide-react';

type SettingsCategory =
  | 'general'
  | 'appearance'
  | 'integrations'
  | 'notifications'
  | 'shelf-of-shame'
  | 'authentication'
  | 'advanced';

export default function SettingsPage() {
  const [activeCategory, setActiveCategory] = useState<SettingsCategory>('general');
  const [publicUrl, setPublicUrl] = useState('');
  const [language, setLanguage] = useState('en');
  const [dateFormat, setDateFormat] = useState('MM/DD/YYYY');
  const [timeFormat, setTimeFormat] = useState('12h');
  const [currency, setCurrency] = useState('USD');
  const [updateCheckEnabled, setUpdateCheckEnabled] = useState(true);
  const [versionTrack, setVersionTrack] = useState('stable');

  // Shelf of Shame Settings
  const [shelfThresholdDays, setShelfThresholdDays] = useState('90');
  const [showShelfBadges, setShowShelfBadges] = useState(true);
  const [shelfNotifications, setShelfNotifications] = useState(true);

  // OIDC Settings
  const [oidcEnabled, setOidcEnabled] = useState(false);
  const [oidcProvider, setOidcProvider] = useState('');
  const [oidcClientId, setOidcClientId] = useState('');
  const [oidcClientSecret, setOidcClientSecret] = useState('');

  // Appearance Settings
  const [primaryColor, setPrimaryColor] = useState('#a855f7');

  const [saved, setSaved] = useState(false);

  const categories = [
    { id: 'general' as const, label: 'General', icon: Settings, description: 'Basic app configuration' },
    { id: 'appearance' as const, label: 'Appearance', icon: Palette, description: 'Theme & display settings' },
    { id: 'integrations' as const, label: 'Integrations', icon: Globe, description: 'External services' },
    { id: 'notifications' as const, label: 'Notifications', icon: Bell, description: 'Alert preferences' },
    { id: 'shelf-of-shame' as const, label: 'Shelf of Shame', icon: BookOpen, description: 'Unplayed games tracking' },
    { id: 'authentication' as const, label: 'Authentication', icon: Shield, description: 'OIDC & login settings' },
    { id: 'advanced' as const, label: 'Advanced', icon: Database, description: 'Updates & advanced options' },
  ];

  // Load saved settings from localStorage on mount
  useEffect(() => {
    const savedUrl = localStorage.getItem('publicRsvpUrl');
    const savedLanguage = localStorage.getItem('language');
    const savedDateFormat = localStorage.getItem('dateFormat');
    const savedTimeFormat = localStorage.getItem('timeFormat');
    const savedCurrency = localStorage.getItem('currency');
    const savedUpdateCheck = localStorage.getItem('updateCheckEnabled');
    const savedVersionTrack = localStorage.getItem('versionTrack');
    const savedShelfThreshold = localStorage.getItem('shelfThresholdDays');
    const savedShowShelfBadges = localStorage.getItem('showShelfBadges');
    const savedShelfNotifications = localStorage.getItem('shelfNotifications');
    const savedOidcEnabled = localStorage.getItem('oidcEnabled');
    const savedOidcProvider = localStorage.getItem('oidcProvider');
    const savedOidcClientId = localStorage.getItem('oidcClientId');
    const savedPrimaryColor = localStorage.getItem('primaryColor');

    if (savedUrl) setPublicUrl(savedUrl);
    else setPublicUrl(window.location.origin);

    if (savedLanguage) setLanguage(savedLanguage);
    if (savedDateFormat) setDateFormat(savedDateFormat);
    if (savedTimeFormat) setTimeFormat(savedTimeFormat);
    if (savedCurrency) setCurrency(savedCurrency);
    if (savedUpdateCheck !== null) setUpdateCheckEnabled(savedUpdateCheck === 'true');
    if (savedVersionTrack) setVersionTrack(savedVersionTrack);
    if (savedShelfThreshold) setShelfThresholdDays(savedShelfThreshold);
    if (savedShowShelfBadges !== null) setShowShelfBadges(savedShowShelfBadges === 'true');
    if (savedShelfNotifications !== null) setShelfNotifications(savedShelfNotifications === 'true');
    if (savedOidcEnabled !== null) setOidcEnabled(savedOidcEnabled === 'true');
    if (savedOidcProvider) setOidcProvider(savedOidcProvider);
    if (savedOidcClientId) setOidcClientId(savedOidcClientId);
    if (savedPrimaryColor) setPrimaryColor(savedPrimaryColor);
  }, []);

  // Apply primary color dynamically whenever it changes
  useEffect(() => {
    // Update CSS variable for rdp-accent-color in globals.css
    document.documentElement.style.setProperty('--rdp-accent-color', primaryColor);
    document.documentElement.style.setProperty('--rdp-accent-background-color', primaryColor);

    // Update all data-theme attributes if needed
    const root = document.documentElement;
    root.setAttribute('data-primary-color', primaryColor);
  }, [primaryColor]);

  const handleSave = () => {
    localStorage.setItem('publicRsvpUrl', publicUrl);
    localStorage.setItem('language', language);
    localStorage.setItem('dateFormat', dateFormat);
    localStorage.setItem('timeFormat', timeFormat);
    localStorage.setItem('currency', currency);
    localStorage.setItem('updateCheckEnabled', updateCheckEnabled.toString());
    localStorage.setItem('versionTrack', versionTrack);
    localStorage.setItem('shelfThresholdDays', shelfThresholdDays);
    localStorage.setItem('showShelfBadges', showShelfBadges.toString());
    localStorage.setItem('shelfNotifications', shelfNotifications.toString());
    localStorage.setItem('oidcEnabled', oidcEnabled.toString());
    localStorage.setItem('oidcProvider', oidcProvider);
    localStorage.setItem('oidcClientId', oidcClientId);
    localStorage.setItem('primaryColor', primaryColor);

    setSaved(true);
    setTimeout(() => setSaved(false), 2000);
  };

  const renderGeneralSettings = () => (
    <div className="space-y-6">
      {/* Public URL Section */}
      <div className="bg-white/5 border border-white/10 rounded-lg p-5">
        <div className="flex items-center gap-2 mb-3">
          <Globe className="text-primary" size={20} />
          <h3 className="text-lg font-bold">Public URL Configuration</h3>
        </div>
        <p className="text-sm text-gray-400 mb-4">
          Set your public URL to generate correct RSVP links for game nights.
        </p>
        <div>
          <label htmlFor="publicUrl" className="block text-sm font-medium text-gray-300 mb-1.5">
            Public URL
          </label>
          <input
            id="publicUrl"
            type="url"
            value={publicUrl}
            onChange={(e) => setPublicUrl(e.target.value)}
            placeholder="https://your-domain.com"
            className="w-full px-3 py-2 bg-[#1a1432] border border-white/10 rounded-lg text-white placeholder-gray-500 focus:outline-none focus:border-primary transition-colors"
          />
          <p className="text-xs text-gray-500 mt-1.5">Example: https://boardgames.myapp.com or http://localhost:3000</p>
        </div>
      </div>

      {/* Language Settings */}
      <div className="bg-white/5 border border-white/10 rounded-lg p-5">
        <div className="flex items-center gap-2 mb-3">
          <Languages className="text-primary" size={20} />
          <h3 className="text-lg font-bold">Language Settings</h3>
        </div>
        <p className="text-sm text-gray-400 mb-4">Set the language for your application.</p>
        <div>
          <label htmlFor="language" className="block text-sm font-medium text-gray-300 mb-1.5">
            Language
          </label>
          <input
            id="language"
            type="text"
            value={language}
            onChange={(e) => setLanguage(e.target.value)}
            placeholder="en"
            className="w-full px-3 py-2 bg-[#1a1432] border border-white/10 rounded-lg text-white placeholder-gray-500 focus:outline-none focus:border-primary transition-colors"
          />
          <p className="text-xs text-gray-500 mt-1.5">Example: en, es, fr</p>
        </div>
      </div>

      {/* Date and Time Settings */}
      <div className="bg-white/5 border border-white/10 rounded-lg p-5">
        <div className="flex items-center gap-2 mb-3">
          <Calendar className="text-primary" size={20} />
          <h3 className="text-lg font-bold">Date and Time Settings</h3>
        </div>
        <p className="text-sm text-gray-400 mb-4">Configure date and time formats to match your preferences.</p>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <div>
            <label htmlFor="dateFormat" className="block text-sm font-medium text-gray-300 mb-1.5">
              Date Format
            </label>
            <input
              id="dateFormat"
              type="text"
              value={dateFormat}
              onChange={(e) => setDateFormat(e.target.value)}
              placeholder="MM/DD/YYYY"
              className="w-full px-3 py-2 bg-[#1a1432] border border-white/10 rounded-lg text-white placeholder-gray-500 focus:outline-none focus:border-primary transition-colors"
            />
            <p className="text-xs text-gray-500 mt-1.5">Example: MM/DD/YYYY, DD/MM/YYYY</p>
          </div>
          <div>
            <label htmlFor="timeFormat" className="block text-sm font-medium text-gray-300 mb-1.5">
              Time Format
            </label>
            <input
              id="timeFormat"
              type="text"
              value={timeFormat}
              onChange={(e) => setTimeFormat(e.target.value)}
              placeholder="12h"
              className="w-full px-3 py-2 bg-[#1a1432] border border-white/10 rounded-lg text-white placeholder-gray-500 focus:outline-none focus:border-primary transition-colors"
            />
            <p className="text-xs text-gray-500 mt-1.5">Example: 12h, 24h</p>
          </div>
        </div>
      </div>

      {/* Currency Settings */}
      <div className="bg-white/5 border border-white/10 rounded-lg p-5">
        <div className="flex items-center gap-2 mb-3">
          <DollarSign className="text-primary" size={20} />
          <h3 className="text-lg font-bold">Currency Settings</h3>
        </div>
        <p className="text-sm text-gray-400 mb-4">Set the currency for financial transactions.</p>
        <div>
          <label htmlFor="currency" className="block text-sm font-medium text-gray-300 mb-1.5">
            Currency
          </label>
          <input
            id="currency"
            type="text"
            value={currency}
            onChange={(e) => setCurrency(e.target.value)}
            placeholder="USD"
            className="w-full px-3 py-2 bg-[#1a1432] border border-white/10 rounded-lg text-white placeholder-gray-500 focus:outline-none focus:border-primary transition-colors"
          />
          <p className="text-xs text-gray-500 mt-1.5">Example: USD, EUR, GBP</p>
        </div>
      </div>
    </div>
  );

  const renderAppearanceSettings = () => (
    <div className="space-y-6">
      <div className="bg-white/5 border border-white/10 rounded-lg p-5">
        <div className="flex items-center gap-2 mb-3">
          <Palette className="text-primary" size={20} />
          <h3 className="text-lg font-bold">Theme Settings</h3>
        </div>
        <p className="text-sm text-gray-400 mb-4">Customize the appearance of your application.</p>

        <div className="space-y-4">
          {/* Primary Color */}
          <div>
            <label htmlFor="primaryColor" className="block text-sm font-medium text-gray-300 mb-3">
              Primary Color
            </label>

            {/* Custom Color Picker */}
            <div>
              <label htmlFor="customColor" className="block text-xs font-medium text-gray-400 mb-2">
                Or choose a custom color
              </label>
              <div className="flex items-center gap-3">
                <input
                  id="customColor"
                  type="color"
                  value={primaryColor}
                  onChange={(e) => setPrimaryColor(e.target.value)}
                  className="h-12 w-20 rounded-lg border border-white/10 bg-[#1a1432] cursor-pointer"
                />
                <div className="flex-1">
                  <input
                    type="text"
                    value={primaryColor}
                    onChange={(e) => setPrimaryColor(e.target.value)}
                    placeholder="#a855f7"
                    className="w-full px-3 py-2 bg-[#1a1432] border border-white/10 rounded-lg text-white placeholder-gray-500 focus:outline-none focus:border-primary transition-colors font-mono text-sm"
                  />
                </div>
              </div>
              <p className="text-xs text-gray-500 mt-2">Enter a hex color code or use the color picker</p>
            </div>

            {/* Preview */}
            <div className="mt-6 p-4 bg-[#1a1432] border border-white/10 rounded-lg">
              <p className="text-xs text-gray-400 mb-3">Preview</p>
              <div className="flex flex-wrap gap-2">
                <button
                  className="px-4 py-2 rounded-lg font-medium transition-all"
                  style={{ backgroundColor: primaryColor, color: 'white' }}
                >
                  Primary Button
                </button>
                <div className="px-4 py-2 rounded-lg border" style={{ borderColor: primaryColor, color: primaryColor }}>
                  Outlined Element
                </div>
                <div
                  className="w-12 h-12 rounded-full"
                  style={{ backgroundColor: `${primaryColor}20`, border: `2px solid ${primaryColor}` }}
                />
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );

  const renderShelfOfShameSettings = () => (
    <div className="space-y-6">
      {/* Threshold Settings */}
      <div className="bg-white/5 border border-white/10 rounded-lg p-5">
        <div className="flex items-center gap-2 mb-3">
          <BookOpen className="text-primary" size={20} />
          <h3 className="text-lg font-bold">Shelf of Shame Tracking</h3>
        </div>
        <p className="text-sm text-gray-400 mb-4">Configure how unplayed games are tracked and displayed.</p>

        <div className="space-y-4">
          {/* Days Threshold */}
          <div>
            <label htmlFor="shelfThreshold" className="block text-sm font-medium text-gray-300 mb-1.5">
              Unplayed Days Threshold
            </label>
            <input
              id="shelfThreshold"
              type="number"
              value={shelfThresholdDays}
              onChange={(e) => setShelfThresholdDays(e.target.value)}
              placeholder="90"
              className="w-full px-3 py-2 bg-[#1a1432] border border-white/10 rounded-lg text-white placeholder-gray-500 focus:outline-none focus:border-primary transition-colors"
            />
            <p className="text-xs text-gray-500 mt-1.5">
              Games unplayed for this many days appear on the Shelf of Shame
            </p>
          </div>

          {/* Show Badges Toggle */}
          <div className="flex items-center justify-between p-3 bg-[#1a1432] rounded-lg border border-white/10">
            <div className="flex-1 pr-3">
              <label htmlFor="showShelfBadges" className="block text-sm font-medium text-gray-300 mb-0.5">
                Show Shelf Badges
              </label>
              <p className="text-xs text-gray-500">Display badges on games in the Shelf of Shame</p>
            </div>
            <button
              onClick={() => setShowShelfBadges(!showShelfBadges)}
              className={`relative inline-flex h-7 w-12 flex-shrink-0 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2 focus:ring-offset-[#0a0416] ${
                showShelfBadges ? 'bg-primary' : 'bg-white/10'
              }`}
            >
              <span
                className={`inline-block h-5 w-5 transform rounded-full bg-white transition-transform ${
                  showShelfBadges ? 'translate-x-6' : 'translate-x-1'
                }`}
              />
            </button>
          </div>

          {/* Notifications Toggle */}
          <div className="flex items-center justify-between p-3 bg-[#1a1432] rounded-lg border border-white/10">
            <div className="flex-1 pr-3">
              <label htmlFor="shelfNotifications" className="block text-sm font-medium text-gray-300 mb-0.5">
                Shelf Reminders
              </label>
              <p className="text-xs text-gray-500">Receive notifications about unplayed games</p>
            </div>
            <button
              onClick={() => setShelfNotifications(!shelfNotifications)}
              className={`relative inline-flex h-7 w-12 shrink-0 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2 focus:ring-offset-[#0a0416] ${
                shelfNotifications ? 'bg-primary' : 'bg-white/10'
              }`}
            >
              <span
                className={`inline-block h-5 w-5 transform rounded-full bg-white transition-transform ${
                  shelfNotifications ? 'translate-x-6' : 'translate-x-1'
                }`}
              />
            </button>
          </div>
        </div>
      </div>
    </div>
  );

  const renderAuthenticationSettings = () => (
    <div className="space-y-6">
      {/* OIDC Settings */}
      <div className="bg-white/5 border border-white/10 rounded-lg p-5">
        <div className="flex items-center gap-2 mb-3">
          <Shield className="text-primary" size={20} />
          <h3 className="text-lg font-bold">OpenID Connect (OIDC)</h3>
        </div>
        <p className="text-sm text-gray-400 mb-4">Configure single sign-on with an OIDC provider.</p>

        <div className="space-y-4">
          {/* Enable OIDC Toggle */}
          <div className="flex items-center justify-between p-3 bg-[#1a1432] rounded-lg border border-white/10">
            <div className="flex-1 pr-3">
              <label htmlFor="oidcEnabled" className="block text-sm font-medium text-gray-300 mb-0.5">
                Enable OIDC Authentication
              </label>
              <p className="text-xs text-gray-500">Use external identity provider for authentication</p>
            </div>
            <button
              onClick={() => setOidcEnabled(!oidcEnabled)}
              className={`relative inline-flex h-7 w-12 flex-shrink-0 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2 focus:ring-offset-[#0a0416] ${
                oidcEnabled ? 'bg-primary' : 'bg-white/10'
              }`}
            >
              <span
                className={`inline-block h-5 w-5 transform rounded-full bg-white transition-transform ${
                  oidcEnabled ? 'translate-x-6' : 'translate-x-1'
                }`}
              />
            </button>
          </div>

          {oidcEnabled && (
            <>
              {/* Provider URL */}
              <div>
                <label htmlFor="oidcProvider" className="block text-sm font-medium text-gray-300 mb-1.5">
                  Provider URL
                </label>
                <input
                  id="oidcProvider"
                  type="url"
                  value={oidcProvider}
                  onChange={(e) => setOidcProvider(e.target.value)}
                  placeholder="https://accounts.example.com"
                  className="w-full px-3 py-2 bg-[#1a1432] border border-white/10 rounded-lg text-white placeholder-gray-500 focus:outline-none focus:border-primary transition-colors"
                />
                <p className="text-xs text-gray-500 mt-1.5">The base URL of your OIDC provider</p>
              </div>

              {/* Client ID */}
              <div>
                <label htmlFor="oidcClientId" className="block text-sm font-medium text-gray-300 mb-1.5">
                  Client ID
                </label>
                <input
                  id="oidcClientId"
                  type="text"
                  value={oidcClientId}
                  onChange={(e) => setOidcClientId(e.target.value)}
                  placeholder="your-client-id"
                  className="w-full px-3 py-2 bg-[#1a1432] border border-white/10 rounded-lg text-white placeholder-gray-500 focus:outline-none focus:border-primary transition-colors"
                />
              </div>

              {/* Client Secret */}
              <div>
                <label htmlFor="oidcClientSecret" className="block text-sm font-medium text-gray-300 mb-1.5">
                  Client Secret
                </label>
                <input
                  id="oidcClientSecret"
                  type="password"
                  value={oidcClientSecret}
                  onChange={(e) => setOidcClientSecret(e.target.value)}
                  placeholder="••••••••••••••••"
                  className="w-full px-3 py-2 bg-[#1a1432] border border-white/10 rounded-lg text-white placeholder-gray-500 focus:outline-none focus:border-primary transition-colors"
                />
                <p className="text-xs text-gray-500 mt-1.5">Keep this secret secure and never share it publicly</p>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );

  const renderAdvancedSettings = () => (
    <div className="space-y-6">
      {/* Update Check Settings */}
      <div className="bg-white/5 border border-white/10 rounded-lg p-5">
        <div className="flex items-center gap-2 mb-3">
          <RefreshCw className="text-primary" size={20} />
          <h3 className="text-lg font-bold">Update Check Settings</h3>
        </div>
        <p className="text-sm text-gray-400 mb-4">Configure how the application checks for updates.</p>

        <div className="space-y-4">
          {/* Enable Update Check Toggle */}
          <div className="flex items-center justify-between p-3 bg-[#1a1432] rounded-lg border border-white/10">
            <div className="flex-1 pr-3">
              <label htmlFor="updateCheckEnabled" className="block text-sm font-medium text-gray-300 mb-0.5">
                Enable Update Check
              </label>
              <p className="text-xs text-gray-500">Automatically check for new versions when the app starts</p>
            </div>
            <button
              onClick={() => setUpdateCheckEnabled(!updateCheckEnabled)}
              className={`relative inline-flex h-7 w-12 flex-shrink-0 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2 focus:ring-offset-[#0a0416] ${
                updateCheckEnabled ? 'bg-primary' : 'bg-white/10'
              }`}
            >
              <span
                className={`inline-block h-5 w-5 transform rounded-full bg-white transition-transform ${
                  updateCheckEnabled ? 'translate-x-6' : 'translate-x-1'
                }`}
              />
            </button>
          </div>

          {/* Version Track Selection */}
          <div>
            <label className="block text-sm font-medium text-gray-300 mb-2">Version Track</label>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-2">
              {/* Stable Track */}
              <button
                onClick={() => setVersionTrack('stable')}
                className={`p-3 rounded-lg border transition-all text-left ${
                  versionTrack === 'stable'
                    ? 'bg-primary/20 border-primary shadow-lg shadow-primary/20'
                    : 'bg-[#1a1432] border-white/10 hover:border-white/20'
                }`}
              >
                <div className="flex items-center gap-2 mb-1">
                  <Check size={16} className={versionTrack === 'stable' ? 'text-primary' : 'text-transparent'} />
                  <span className={`text-sm font-medium ${versionTrack === 'stable' ? 'text-white' : 'text-gray-300'}`}>
                    Stable
                  </span>
                </div>
                <p className="text-xs text-gray-400">Recommended for production use.</p>
              </button>

              {/* Beta Track */}
              <button
                onClick={() => setVersionTrack('beta')}
                className={`p-3 rounded-lg border transition-all text-left ${
                  versionTrack === 'beta'
                    ? 'bg-primary/20 border-primary shadow-lg shadow-primary/20'
                    : 'bg-[#1a1432] border-white/10 hover:border-white/20'
                }`}
              >
                <div className="flex items-center gap-2 mb-1">
                  <Check size={16} className={versionTrack === 'beta' ? 'text-primary' : 'text-transparent'} />
                  <span className={`text-sm font-medium ${versionTrack === 'beta' ? 'text-white' : 'text-gray-300'}`}>
                    Beta
                  </span>
                </div>
                <p className="text-xs text-gray-400">Early access to new features.</p>
              </button>

              {/* Development Track */}
              <button
                onClick={() => setVersionTrack('dev')}
                className={`p-3 rounded-lg border transition-all text-left ${
                  versionTrack === 'dev'
                    ? 'bg-primary/20 border-primary shadow-lg shadow-primary/20'
                    : 'bg-[#1a1432] border-white/10 hover:border-white/20'
                }`}
              >
                <div className="flex items-center gap-2 mb-1">
                  <Check size={16} className={versionTrack === 'dev' ? 'text-primary' : 'text-transparent'} />
                  <span className={`text-sm font-medium ${versionTrack === 'dev' ? 'text-white' : 'text-gray-300'}`}>
                    Development
                  </span>
                </div>
                <p className="text-xs text-gray-400">Latest experimental changes.</p>
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Community Links */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        <a
          href="https://github.com/yourusername/yourrepo/issues/new"
          target="_blank"
          rel="noopener noreferrer"
          className="bg-white/5 border border-white/10 hover:border-red-500/50 rounded-lg p-4 transition-colors group"
        >
          <div className="flex items-start gap-3">
            <div className="p-2 bg-red-500/20 rounded-lg shrink-0">
              <Bug className="text-red-400" size={20} />
            </div>
            <div className="flex-1">
              <h4 className="font-bold text-sm mb-1">Report a Bug</h4>
              <p className="text-xs text-gray-400">Help us improve by reporting bugs on GitHub.</p>
            </div>
          </div>
        </a>

        <a
          href="https://crowdin.com/project/yourproject"
          target="_blank"
          rel="noopener noreferrer"
          className="bg-white/5 border border-white/10 hover:border-blue-500/50 rounded-lg p-4 transition-colors group"
        >
          <div className="flex items-start gap-3">
            <div className="p-2 bg-blue-500/20 rounded-lg shrink-0">
              <FileText className="text-blue-400" size={20} />
            </div>
            <div className="flex-1">
              <h4 className="font-bold text-sm mb-1">Help Translate</h4>
              <p className="text-xs text-gray-400">Contribute translations on Crowdin.</p>
            </div>
          </div>
        </a>
      </div>
    </div>
  );

  const renderContent = () => {
    switch (activeCategory) {
      case 'general':
        return renderGeneralSettings();
      case 'appearance':
        return renderAppearanceSettings();
      case 'shelf-of-shame':
        return renderShelfOfShameSettings();
      case 'authentication':
        return renderAuthenticationSettings();
      case 'advanced':
        return renderAdvancedSettings();
      default:
        return renderGeneralSettings();
    }
  };

  return (
    <div className="min-h-full">
      {/* Header */}
      <div className="p-6 md:p-8 border-b border-white/10">
        <div className="flex items-center gap-3 mb-2">
          <Settings className="text-primary" size={28} />
          <h1 className="text-3xl md:text-4xl text-white">SETTINGS</h1>
        </div>
        <p className="text-white/60">Configure your application preferences and integrations</p>
      </div>

      {/* Main Content with Sidebar */}
      <div className="flex flex-col lg:flex-row">
        {/* Sidebar Navigation */}
        <div className="w-full lg:w-64 border-b lg:border-b-0 lg:border-r border-white/10 p-4 lg:p-6">
          <nav className="space-y-1">
            {categories.map((category) => {
              const Icon = category.icon;
              const isActive = activeCategory === category.id;

              return (
                <button
                  key={category.id}
                  onClick={() => setActiveCategory(category.id)}
                  className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg transition-all text-left ${
                    isActive
                      ? 'bg-primary/20 border border-primary/50 text-white'
                      : 'text-white/70 hover:bg-white/5 border border-transparent hover:text-white'
                  }`}
                >
                  <Icon size={18} className={isActive ? 'text-primary' : 'text-white/50'} />
                  <div className="flex-1 min-w-0">
                    <div className="text-sm font-medium">{category.label}</div>
                    <div className="text-xs text-white/50 truncate">{category.description}</div>
                  </div>
                  {isActive && <ChevronRight size={16} className="text-primary flex-shrink-0" />}
                </button>
              );
            })}
          </nav>
        </div>

        {/* Content Area */}
        <div className="flex-1 p-6 md:p-8">
          {renderContent()}

          {/* Save Button - Fixed at Bottom of Content */}
          <div className="mt-8 pt-6 border-t border-white/10 sticky bottom-0 bg-[#0a0416]/95 backdrop-blur-sm">
            <div className="flex items-center justify-between flex-wrap gap-3">
              <div>
                <h3 className="font-bold text-base mb-0.5 text-white">Save Your Settings</h3>
                <p className="text-xs text-gray-400">Changes will be applied immediately</p>
              </div>
              <button
                onClick={handleSave}
                className={`px-6 py-2.5 rounded-lg font-medium transition-all flex items-center gap-2 text-sm ${
                  saved
                    ? 'bg-green-500 text-white'
                    : 'bg-primary hover:bg-[#9333ea] text-white shadow-lg shadow-primary/20'
                }`}
              >
                {saved ? (
                  <>
                    <Check size={18} />
                    Saved!
                  </>
                ) : (
                  <>
                    <Save size={18} />
                    Save All Settings
                  </>
                )}
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
