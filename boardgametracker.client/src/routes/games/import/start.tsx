import { Bars } from 'react-loading-icons';
import { useTranslation } from 'react-i18next';
import { createFileRoute, useNavigate, useRouter } from '@tanstack/react-router';
import type { AnyFieldApi } from '@tanstack/react-form';

import { zodValidator } from '@/utils/zodValidator';
import { handleFormSubmit } from '@/utils/formUtils';
import { BggUserNameSchema } from '@/models';
import { useAppForm } from '@/hooks/form';
import { BgtInputField } from '@/components/BgtForm';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtCenteredCard } from '@/components/BgtCard/BgtCenteredCard';
import BgtButton from '@/components/BgtButton/BgtButton';

export const Route = createFileRoute('/games/import/start')({
  component: RouteComponent,
});

function RouteComponent() {
  const { t } = useTranslation();
  const router = useRouter();
  const navigate = useNavigate();

  const form = useAppForm({
    defaultValues: {
      username: '',
    },
    onSubmit: async ({ value }) => {
      const validatedData = BggUserNameSchema.parse(value);
      navigate({ to: `/games/import/list/${validatedData.username}` });
    },
  });

  const isLoading = false;

  return (
    <BgtPage>
      <BgtPageContent>
        <BgtCenteredCard title={t('games.import.start.title')} className="max-w-[600px]">
          <form
            onSubmit={handleFormSubmit(form)}
          >
            <div className="flex flex-col gap-5 w-full">
              <div>{t('games.import.start.description')}</div>
              <form.Field name="username" validators={zodValidator(BggUserNameSchema, 'username')}>
                {(field: AnyFieldApi) => (
                  <BgtInputField
                    field={field}
                    disabled={isLoading}
                    label={t('games.import.start.bgg-username.label')}
                    type="text"
                  />
                )}
              </form.Field>
              <div className="flex flex-row gap-2">
                <BgtButton
                  variant="cancel"
                  type="button"
                  disabled={isLoading}
                  className="flex-none"
                  onClick={() => router.history.back()}
                >
                  {t('common.cancel')}
                </BgtButton>
                <BgtButton type="submit" disabled={isLoading} className="flex-1" variant="primary">
                  {isLoading && <Bars className="size-4" />}
                  {t('games.import.start.button')}
                </BgtButton>
              </div>
            </div>
          </form>
        </BgtCenteredCard>
      </BgtPageContent>
    </BgtPage>
  );
}
