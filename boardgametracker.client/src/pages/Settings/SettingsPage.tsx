import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';

import { usePage } from '../../hooks/usePage';
import BgtPageHeader from '../../components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '../../components/BgtLayout/BgtPageContent';
import { BgtPage } from '../../components/BgtLayout/BgtPage';

import { ToLogLevel } from '@/utils/numberUtils';
import { Settings, SettingsSchema } from '@/models';
import { useSettings } from '@/hooks/useSettings';
import { BgtHeading } from '@/components/BgtHeading/BgtHeading';
import { BgtSelect } from '@/components/BgtForm/BgtSelect';
import { BgtInputField } from '@/components/BgtForm/BgtInputField';
import { BgtCard } from '@/components/BgtCard/BgtCard';
import BgtButton from '@/components/BgtButton/BgtButton';
import GitHubIcon from '@/assets/icons/github.svg?react';
import CrowdinIcon from '@/assets/icons/crowdin.svg?react';

export const SettingsPage = () => {
  const { t, i18n } = useTranslation();
  const { pageTitle } = usePage();
  const { settings, saveSettings, isPending, languages, environment } = useSettings();

  const { handleSubmit, control } = useForm<Settings>({
    resolver: zodResolver(SettingsSchema),
    defaultValues: settings.data,
  });

  if (settings.data === undefined || languages.data === undefined || environment.data === undefined) return null;

  const onSubmit = async (data: Settings) => {
    await saveSettings(data);
    i18n.changeLanguage(data.uiLanguage);
  };

  return (
    <BgtPage>
      <BgtPageHeader header={t(pageTitle)} actions={[]} />
      <BgtPageContent>
        <BgtCard className="p-4">
          <form onSubmit={(event) => void handleSubmit(onSubmit)(event)}>
            <div className="flex flex-row justify-between">
              <BgtHeading size="6">{t('settings.titles.localisation')}</BgtHeading>
              <BgtButton size="1" type="submit" disabled={isPending}>
                {t('common.save')}
              </BgtButton>
            </div>
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
              <BgtSelect
                disabled={isPending}
                control={control}
                label={t('settings.ui-language.label')}
                name="uiLanguage"
                items={languages.data.map((value) => ({
                  label: t(`languages.${value.translationKey}`),
                  value: value.key,
                  image: null,
                }))}
              />
              <BgtInputField
                disabled={isPending}
                type="text"
                control={control}
                name="dateFormat"
                label={t('settings.date-format.label')}
              />
              <BgtInputField
                disabled={isPending}
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
              <BgtButton size="1" type="submit" disabled={isPending}>
                {t('common.save')}
              </BgtButton>
            </div>
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
              <BgtInputField
                disabled={isPending}
                type="text"
                control={control}
                name="currency"
                label={t('settings.currency.label')}
              />
              <BgtInputField
                disabled={isPending}
                type="text"
                control={control}
                name="decimalSeparator"
                label={t('settings.decimal-separator.label')}
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
                <div className="text-end md:text-start">{environment.data.environmentName}</div>
                <div className="text-end md:text-start">{environment.data.port}</div>
                <div className="text-end md:text-start">
                  {environment.data.enableStatistics ? t('common.enabled') : t('common.disabled')}
                </div>
                <div className="text-end md:text-start">{t(ToLogLevel(environment.data.logLevel))}</div>
                <div className="text-end md:text-start">{environment.data.version}</div>
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
};
