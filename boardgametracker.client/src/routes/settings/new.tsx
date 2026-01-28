import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import { createFileRoute } from '@tanstack/react-router';
import { useForm } from '@tanstack/react-form';

import { useToasts } from '../-hooks/useToasts';

import { useSettingsData } from './-hooks/useSettingsData';
import { ShelfOfShameSettings } from './-components/ShelfOfShameSettings';
import { SettingsSidebar, SettingsCategory } from './-components/SettingsSidebar';
import { GeneralSettings } from './-components/GeneralSettings';
import { AuthenticationSettings } from './-components/AuthenticationSettings';
import { AppearanceSettings } from './-components/AppearanceSettings';
import { AdvancedSettings } from './-components/AdvancedSettings';

import { getSettings, getLanguages, getEnvironment } from '@/services/queries/settings';
import { SettingsSchema } from '@/models';
import { BgtLoadingSpinner } from '@/components/BgtLoadingSpinner/BgtLoadingSpinner';
import BgtPageHeader from '@/components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import BgtButton from '@/components/BgtButton/BgtButton';
import CogIcon from '@/assets/icons/cog.svg?react';

export const Route = createFileRoute('/settings/new')({
  component: RouteComponent,
  loader: ({ context: { queryClient } }) => {
    queryClient.prefetchQuery(getSettings());
    queryClient.prefetchQuery(getLanguages());
    queryClient.prefetchQuery(getEnvironment());
  },
});

function RouteComponent() {
  const [activeCategory, setActiveCategory] = useState<SettingsCategory>('general');
  const { errorToast, successToast } = useToasts();
  const { t } = useTranslation();

  const onSaveError = () => {
    errorToast('settings.save.failed');
  };

  const onSaveSuccess = () => {
    successToast('settings.save.successfull');
  };

  const { settings, saveSettings, isLoading, languages } = useSettingsData({ onSaveSuccess, onSaveError });

  const form = useForm({
    defaultValues: {
      uiLanguage: settings?.uiLanguage ?? '',
      dateFormat: settings?.dateFormat ?? '',
      timeFormat: settings?.timeFormat ?? '',
      currency: settings?.currency ?? '',
      statistics: settings?.statistics ?? false,
      updateCheckEnabled: settings?.updateCheckEnabled ?? true,
      versionTrack: settings?.versionTrack ?? 'stable',
      shelfOfShameEnabled: settings?.shelfOfShameEnabled ?? false,
      shelfOfShameMonthsLimit: settings?.shelfOfShameMonthsLimit ?? 3,
      oidcEnabled: settings?.oidcEnabled ?? false,
      oidcProvider: settings?.oidcProvider ?? '',
      oidcClientId: settings?.oidcClientId ?? '',
      oidcClientSecret: settings?.oidcClientSecret ?? '',
      publicUrl: settings?.publicUrl ?? '',
      primaryColor: settings?.primaryColor ?? '#a855f7',
    },
    onSubmit: async ({ value }) => {
      const validatedData = SettingsSchema.parse(value);
      await saveSettings(validatedData);
    },
  });

  if (settings === undefined) {
    return (
      <div className="min-h-full flex items-center justify-center">
        <BgtLoadingSpinner />
      </div>
    );
  }

  const renderContent = () => {
    switch (activeCategory) {
      case 'general':
        return <GeneralSettings form={form} languages={languages} disabled={isLoading} />;
      case 'appearance':
        return <AppearanceSettings form={form} disabled={isLoading} />;
      case 'shelf-of-shame':
        return <ShelfOfShameSettings form={form} disabled={isLoading} />;
      case 'authentication':
        return <AuthenticationSettings form={form} disabled={isLoading} />;
      case 'advanced':
        return <AdvancedSettings form={form} disabled={isLoading} />;
      default:
        return <GeneralSettings form={form} languages={languages} disabled={isLoading} />;
    }
  };

  return (
    <BgtPage>
      <BgtPageHeader header={'Settings'} icon={CogIcon} />
      <BgtPageContent>
        <div className="flex flex-col lg:flex-row">
          <SettingsSidebar activeCategory={activeCategory} onCategoryChange={setActiveCategory} />

          <div className="flex-1">
            <form
              onSubmit={(e) => {
                e.preventDefault();
                e.stopPropagation();
                form.handleSubmit();
              }}
            >
              <div className="flex flex-col gap-4 xl:gap-6 lg:pl-4 xl:pl-6 pt-4 lg:pt-0">{renderContent()}</div>
              <div className="mt-6 pt-4 lg:ml-4 xl:ml-6 border-t border-white/10">
                <div className="flex justify-between flex-wrap gap-3 items-start">
                  <BgtButton onClick={form.handleSubmit} type="submit" disabled={isLoading}>
                    {t('settings.save.button')}
                  </BgtButton>
                </div>
              </div>
            </form>
          </div>
        </div>
      </BgtPageContent>
    </BgtPage>
  );
}
