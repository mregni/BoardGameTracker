import { Bars } from 'react-loading-icons';
import { useTranslation } from 'react-i18next';
import { createFileRoute, useNavigate, useRouter } from '@tanstack/react-router';
import { useForm } from '@tanstack/react-form';

import { BggUserNameSchema } from '@/models';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtFormField, BgtInputField } from '@/components/BgtForm';
import { BgtCenteredCard } from '@/components/BgtCard/BgtCenteredCard';
import BgtButton from '@/components/BgtButton/BgtButton';

export const Route = createFileRoute('/games/import/start')({
  component: RouteComponent,
});

function RouteComponent() {
  const { t } = useTranslation();
  const router = useRouter();
  const navigate = useNavigate();

  const form = useForm({
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
            onSubmit={(e) => {
              e.preventDefault();
              e.stopPropagation();
              form.handleSubmit();
            }}
          >
            <div className="flex flex-col gap-5 w-full">
              <div>{t('games.import.start.description')}</div>
              <BgtFormField form={form} name="username" schema={BggUserNameSchema.shape.username}>
                {(field) => (
                  <BgtInputField
                    field={field}
                    disabled={isLoading}
                    label={t('games.import.start.bgg-username.label')}
                    type="text"
                  />
                )}
              </BgtFormField>
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
