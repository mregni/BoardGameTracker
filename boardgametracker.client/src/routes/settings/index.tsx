import { useTranslation } from 'react-i18next';
import { createFileRoute } from '@tanstack/react-router';
import { useForm } from '@tanstack/react-form';

import { useToasts } from '../-hooks/useToasts';

import { useSettingsData } from './-hooks/useSettingsData';

import { ToLogLevel } from '@/utils/numberUtils';
import { getSettings, getLanguages, getEnvironment } from '@/services/queries/settings';
import { SettingsSchema } from '@/models';
import BgtPageHeader from '@/components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtHeading } from '@/components/BgtHeading/BgtHeading';
import { BgtFormField, BgtSelect, BgtInputField } from '@/components/BgtForm';
import { BgtCard } from '@/components/BgtCard/BgtCard';
import BgtButton from '@/components/BgtButton/BgtButton';
import GitHubIcon from '@/assets/icons/github.svg?react';
import CrowdinIcon from '@/assets/icons/crowdin.svg?react';
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
  const { t, i18n } = useTranslation();
  const { errorToast, successToast } = useToasts();

  const onSaveError = () => {
    errorToast('settings.save.failed');
  };

  const onSaveSuccess = () => {
    successToast('settings.save.successfull');
  };

  const { settings, saveSettings, isLoading, languages, environment } = useSettingsData({ onSaveSuccess, onSaveError });

  const form = useForm({
    defaultValues: {
      uiLanguage: settings?.uiLanguage ?? '',
      dateFormat: settings?.dateFormat ?? '',
      timeFormat: settings?.timeFormat ?? '',
      currency: settings?.currency ?? '',
    },
    onSubmit: async ({ value }) => {
      const validatedData = SettingsSchema.parse(value);
      if (settings) {
        await saveSettings({ ...settings, ...validatedData });
        i18n.changeLanguage(validatedData.uiLanguage);
      }
    },
  });

  if (settings === undefined || environment === undefined) return null;

  return (
    <BgtPage>
      <BgtPageHeader header={t('common.settings')} icon={CogIcon} />
      <BgtPageContent>
        <BgtCard className="p-4">
          <form
            onSubmit={(e) => {
              e.preventDefault();
              e.stopPropagation();
              form.handleSubmit();
            }}
          >
            <div className="flex flex-row justify-between">
              <BgtHeading size="6">{t('settings.titles.localisation')}</BgtHeading>
              <BgtButton size="1" type="submit" disabled={isLoading} variant="primary">
                {t('common.save')}
              </BgtButton>
            </div>
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
              <BgtFormField form={form} name="uiLanguage" schema={SettingsSchema.shape.uiLanguage}>
                {(field) => (
                  <BgtSelect
                    field={field}
                    disabled={isLoading}
                    label={t('settings.ui-language.label')}
                    items={languages.map((value) => ({
                      label: t(`languages.${value.translationKey}`),
                      value: value.key,
                      image: null,
                    }))}
                  />
                )}
              </BgtFormField>
              <BgtFormField form={form} name="dateFormat" schema={SettingsSchema.shape.dateFormat}>
                {(field) => (
                  <BgtInputField
                    field={field}
                    disabled={isLoading}
                    type="text"
                    label={t('settings.date-format.label')}
                  />
                )}
              </BgtFormField>
              <BgtFormField form={form} name="timeFormat" schema={SettingsSchema.shape.timeFormat}>
                {(field) => (
                  <BgtInputField
                    field={field}
                    disabled={isLoading}
                    type="text"
                    label={t('settings.time-format.label')}
                  />
                )}
              </BgtFormField>
            </div>
          </form>
        </BgtCard>
        <BgtCard className="p-4">
          <form
            onSubmit={(e) => {
              e.preventDefault();
              e.stopPropagation();
              form.handleSubmit();
            }}
          >
            <div className="flex flex-row justify-between">
              <BgtHeading size="6">{t('settings.titles.currency')} </BgtHeading>
              <BgtButton size="1" type="submit" disabled={isLoading} variant="primary">
                {t('common.save')}
              </BgtButton>
            </div>
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
              <BgtFormField form={form} name="currency" schema={SettingsSchema.shape.currency}>
                {(field) => (
                  <BgtInputField field={field} disabled={isLoading} type="text" label={t('settings.currency.label')} />
                )}
              </BgtFormField>
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
              <BgtButton
                onClick={() => window.open('https://github.com/mregni/BoardGameTracker/issues')}
                variant="primary"
              >
                <GitHubIcon className="size-4" />
                {t('settings.feature-request')}
              </BgtButton>
              <BgtButton onClick={() => window.open('https://crowdin.com/project/boardgametracker')} variant="primary">
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
