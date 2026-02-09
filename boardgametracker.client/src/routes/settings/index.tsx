import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import { createFileRoute } from '@tanstack/react-router';
import { useForm } from '@tanstack/react-form';

import { useToasts } from '../-hooks/useToasts';

import { useSettingsData } from './-hooks/useSettingsData';
import { ShelfOfShameSettings } from './-components/ShelfOfShameSettings';
import { SettingsSidebar, SettingsCategory } from './-components/SettingsSidebar';
import { GeneralSettings } from './-components/GeneralSettings';
import { GameNightsSettings } from './-components/GameNightsSettings';
import { AdvancedSettings } from './-components/AdvancedSettings';

import { getSettings, getLanguages, getEnvironment } from '@/services/queries/settings';
import { Settings, SettingsSchema } from '@/models';
import { BgtLoadingSpinner } from '@/components/BgtLoadingSpinner/BgtLoadingSpinner';
import BgtPageHeader from '@/components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import BgtButton from '@/components/BgtButton/BgtButton';
import CogIcon from '@/assets/icons/cog.svg?react';

export const Route = createFileRoute('/settings/')({
  component: RouteComponent,
  loader: ({ context: { queryClient } }) => {
    queryClient.prefetchQuery(getSettings());
    queryClient.prefetchQuery(getLanguages());
    queryClient.prefetchQuery(getEnvironment());
  },
});

function RouteComponent() {
  const { errorToast, successToast } = useToasts();

  const onSaveError = () => {
    errorToast('settings.save.failed');
  };

  const onSaveSuccess = () => {
    successToast('settings.save.successfull');
  };

  const { settings, saveSettings, isLoading, languages } = useSettingsData({ onSaveSuccess, onSaveError });

  if (settings === undefined) {
    return (
      <div className="min-h-full flex items-center justify-center">
        <BgtLoadingSpinner />
      </div>
    );
  }

  return (
    <SettingsPageContent settings={settings} languages={languages} isLoading={isLoading} saveSettings={saveSettings} />
  );
}

interface SettingsPageContentProps {
  settings: Settings;
  languages: { key: string; translationKey: string }[];
  isLoading: boolean;
  saveSettings: (settings: Settings) => Promise<Settings>;
}

function SettingsPageContent({ settings, languages, isLoading, saveSettings }: SettingsPageContentProps) {
  const [activeCategory, setActiveCategory] = useState<SettingsCategory>('general');
  const { t } = useTranslation();

  const form = useForm({
    defaultValues: {
      uiLanguage: settings.uiLanguage,
      dateFormat: settings.dateFormat,
      timeFormat: settings.timeFormat,
      currency: settings.currency,
      statistics: settings.statistics,
      updateCheckEnabled: settings.updateCheckEnabled,
      versionTrack: settings.versionTrack,
      shelfOfShameEnabled: settings.shelfOfShameEnabled,
      shelfOfShameMonthsLimit: settings.shelfOfShameMonthsLimit,
      publicUrl: settings.publicUrl,
      gameNightsEnabled: settings.gameNightsEnabled,
    },
    onSubmit: async ({ value }) => {
      const validatedData = SettingsSchema.parse(value);
      await saveSettings(validatedData);
    },
  });

  const renderContent = () => {
    switch (activeCategory) {
      case 'general':
        return <GeneralSettings form={form} languages={languages} disabled={isLoading} />;
      case 'shelf-of-shame':
        return <ShelfOfShameSettings form={form} disabled={isLoading} />;
      case 'game-nights':
        return <GameNightsSettings form={form} disabled={isLoading} />;
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
