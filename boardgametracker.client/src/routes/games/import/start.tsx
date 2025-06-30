import { Bars } from 'react-loading-icons';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { createFileRoute, useNavigate, useRouter } from '@tanstack/react-router';
import { zodResolver } from '@hookform/resolvers/zod';

import { BggUserName, BggUserNameSchema } from '@/models';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtInputField } from '@/components/BgtForm/BgtInputField';
import { BgtCenteredCard } from '@/components/BgtCard/BgtCenteredCard';
import BgtButton from '@/components/BgtButton/BgtButton';

export const Route = createFileRoute('/games/import/start')({
  component: RouteComponent,
});

function RouteComponent() {
  const { t } = useTranslation();
  const router = useRouter();
  const navigate = useNavigate();

  const { handleSubmit, control } = useForm<BggUserName>({
    resolver: zodResolver(BggUserNameSchema),
    defaultValues: {
      username: '',
    },
  });

  const onSubmit = (data: BggUserName) => {
    navigate({ to: `/games/import/list/${data.username}` });
  };

  const isLoading = false;

  return (
    <BgtPage>
      <BgtPageContent>
        <BgtCenteredCard title={t('games.import.start.title')} className="max-w-[600px]">
          <form onSubmit={(event) => void handleSubmit(onSubmit)(event)}>
            <div className="flex flex-col gap-5 w-full">
              <div>{t('games.import.start.description')}</div>
              <BgtInputField
                disabled={isLoading}
                label={t('games.import.start.bgg-username.label')}
                name="username"
                type="text"
                control={control}
              />
              <div className="flex flex-row gap-2">
                <BgtButton
                  variant="outline"
                  type="button"
                  disabled={isLoading}
                  className="flex-none"
                  onClick={() => router.history.back()}
                >
                  {t('common.cancel')}
                </BgtButton>
                <BgtButton type="submit" disabled={isLoading} className="flex-1" variant="soft">
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
