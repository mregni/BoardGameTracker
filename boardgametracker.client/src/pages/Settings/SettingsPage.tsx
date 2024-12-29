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

export const SettingsPage = () => {
  const { t } = useTranslation();
  const { pageTitle } = usePage();
  const { settings, saveSettings, isPending, languages, environment } = useSettings();

  const { handleSubmit, control } = useForm<Settings>({
    resolver: zodResolver(SettingsSchema),
    defaultValues: settings.data,
  });

  if (settings.data === undefined || languages.data === undefined || environment.data === undefined) return null;

  const onSubmit = async (data: Settings) => {
    console.log(data);
    await saveSettings(data);
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
                label={t('game.state.label')}
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
          <div className="flex flex-row gap-3 pt-3">
            <div>
              <div>{t('settings.environment.name')}</div>
              <div>{t('settings.environment.port')}</div>
              <div>{t('settings.environment.statistics')}</div>
              <div>{t('settings.environment.log-level')}</div>
            </div>
            <div>
              <div>{environment.data.environmentName}</div>
              <div>{environment.data.port}</div>
              <div>{environment.data.enableStatistics ? t('common.enabled') : t('common.disabled')}</div>
              <div>{t(ToLogLevel(environment.data.logLevel))}</div>
            </div>
          </div>
        </BgtCard>
      </BgtPageContent>
    </BgtPage>
  );
};
