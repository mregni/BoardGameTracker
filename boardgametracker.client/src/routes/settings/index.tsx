import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { createFileRoute } from '@tanstack/react-router';
import { zodResolver } from '@hookform/resolvers/zod';

import { useToasts } from '../-hooks/useToasts';

import { useSettingsData } from './-hooks/useSettingsData';

import { ToLogLevel } from '@/utils/numberUtils';
import { getSettings, getLanguages, getEnvironment } from '@/services/queries/settings';
import { Settings, SettingsSchema } from '@/models';
import BgtPageHeader from '@/components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtHeading } from '@/components/BgtHeading/BgtHeading';
import { BgtSelect } from '@/components/BgtForm/BgtSelect';
import { BgtInputField } from '@/components/BgtForm/BgtInputField';
import { BgtCard } from '@/components/BgtCard/BgtCard';
import BgtButton from '@/components/BgtButton/BgtButton';
import GitHubIcon from '@/assets/icons/github.svg?react';
import CrowdinIcon from '@/assets/icons/crowdin.svg?react';

export const Route = createFileRoute('/settings/')({
  component: RouteComponent,
  loader: ({ context: { queryClient } }) => {
    queryClient.prefetchQuery(getSettings());
    queryClient.prefetchQuery(getLanguages());
    queryClient.prefetchQuery(getEnvironment());
  },
});

function RouteComponent() {
  const { t, i18n } = useTranslation();
  const { errorToast, successToast } = useToasts();

  const onSaveError = () => {
    errorToast('settings.save.failed');
  };

  const onSaveSuccess = () => {
    successToast('settings.save.successfull');
  };

  const { settings, saveSettings, isLoading, languages, environment } = useSettingsData({ onSaveSuccess, onSaveError });

  const { handleSubmit, control } = useForm<Settings>({
    resolver: zodResolver(SettingsSchema),
    defaultValues: settings,
  });

  if (settings === undefined || environment === undefined) return null;

  const onSubmit = async (data: Settings) => {
    await saveSettings(data);
    i18n.changeLanguage(data.uiLanguage);
  };

  return (
    <BgtPage>
      <BgtPageHeader header={t('common.settings')} actions={[]} />
      <BgtPageContent>
        <BgtCard className="p-4">
          <form onSubmit={(event) => void handleSubmit(onSubmit)(event)}>
            <div className="flex flex-row justify-between">
              <BgtHeading size="6">{t('settings.titles.localisation')}</BgtHeading>
              <BgtButton size="1" type="submit" disabled={isLoading}>
                {t('common.save')}
              </BgtButton>
            </div>
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
              <BgtSelect
                disabled={isLoading}
                control={control}
                label={t('settings.ui-language.label')}
                name="uiLanguage"
                items={languages.map((value) => ({
                  label: t(`languages.${value.translationKey}`),
                  value: value.key,
                  image: null,
                }))}
              />
              <BgtInputField
                disabled={isLoading}
                type="text"
                control={control}
                name="dateFormat"
                label={t('settings.date-format.label')}
              />
              <BgtInputField
                disabled={isLoading}
                type="text"
                control={control}
                name="timeFormat"
                label={t('settings.time-format.label')}
              />
            </div>
          </form>
        </BgtCard>
        <BgtCard className="p-4">
          <form onSubmit={(event) => void handleSubmit(onSubmit)(event)}>
            <div className="flex flex-row justify-between">
              <BgtHeading size="6">{t('settings.titles.currency')} </BgtHeading>
              <BgtButton size="1" type="submit" disabled={isLoading}>
                {t('common.save')}
              </BgtButton>
            </div>
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
              <BgtInputField
                disabled={isLoading}
                type="text"
                control={control}
                name="currency"
                label={t('settings.currency.label')}
              />
            </div>
          </form>
        </BgtCard>
        <BgtCard className="p-4">
          <BgtHeading size="6">{t('settings.titles.environment')}</BgtHeading>
          <div className="flex flex-col md:flex-row gap-3 justify-between">
            <div className="flex flex-row gap-3 pt-3 md:justify-start justify-between">
              <div className="text-gray-500">
                <div>{t('settings.environment.name')}</div>
                <div>{t('settings.environment.port')}</div>
                <div>{t('settings.environment.statistics')}</div>
                <div>{t('settings.environment.log-level')}</div>
                <div>{t('settings.environment.version')}</div>
              </div>
              <div>
                <div className="text-end md:text-start">{environment.environmentName}</div>
                <div className="text-end md:text-start">{environment.port}</div>
                <div className="text-end md:text-start">
                  {environment.enableStatistics ? t('common.enabled') : t('common.disabled')}
                </div>
                <div className="text-end md:text-start">{t(ToLogLevel(environment.logLevel))}</div>
                <div className="text-end md:text-start">{environment.version}</div>
              </div>
            </div>
            <div className="flex flex-col gap-2">
              <BgtButton onClick={() => window.open('https://github.com/mregni/BoardGameTracker/issues')}>
                <GitHubIcon className="size-4" />
                {t('settings.feature-request')}
              </BgtButton>
              <BgtButton onClick={() => window.open('https://crowdin.com/project/boardgametracker')}>
                <CrowdinIcon className="size-4" />
                {t('settings.translations')}
              </BgtButton>
            </div>
          </div>
        </BgtCard>
      </BgtPageContent>
    </BgtPage>
  );
}
