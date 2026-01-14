import { useTranslation } from 'react-i18next';
import { useCallback } from 'react';
import { useRouter } from '@tanstack/react-router';
import { useForm } from '@tanstack/react-form';

import { useImageUpload } from '../-hooks/useImageUpload';
import { useGameForm } from '../-hooks/useGameForm';

import { GameFormTimeFields } from './GameFormTimeFields';
import { GameFormPlayerFields } from './GameFormPlayerFields';
import { GameFormBasicFields } from './GameFormBasicFields';

import { toInputDate } from '@/utils/dateUtils';
import { CreateGame, CreateGameSchema } from '@/models/Games/CreateGame';
import { Game } from '@/models';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtImageSelector } from '@/components/BgtForm/BgtImageSelector';
import { BgtFormField, BgtSwitch } from '@/components/BgtForm';
import { BgtCenteredCard } from '@/components/BgtCard/BgtCenteredCard';
import BgtButton from '@/components/BgtButton/BgtButton';

interface Props {
  onClick: (data: CreateGame) => Promise<void>;
  buttonText: string;
  disabled: boolean;
  game?: Game;
  title: string;
}

export const GameForm = (props: Props) => {
  const { onClick, buttonText, disabled, game, title } = props;
  const { settings } = useGameForm();
  const { t } = useTranslation();
  const router = useRouter();
  const { poster, setPoster, uploadPoster } = useImageUpload(game?.image);

  const form = useForm({
    defaultValues: {
      id: game?.id ?? undefined,
      title: game?.title ?? '',
      hasScoring: game?.hasScoring ?? true,
      description: game?.description ?? '',
      state: game?.state ?? 0,
      yearPublished: game?.yearPublished ?? undefined,
      maxPlayers: game?.maxPlayers ?? undefined,
      minPlayers: game?.minPlayers ?? undefined,
      maxPlayTime: game?.maxPlayTime ?? undefined,
      minPlayTime: game?.minPlayTime ?? undefined,
      minAge: game?.minAge ?? undefined,
      bggId: game?.bggId ?? undefined,
      buyingPrice: game?.buyingPrice ?? 0,
      additionDate: toInputDate(game?.additionDate ?? undefined, true),
      image: game?.image ?? null,
    },
    onSubmit: async ({ value }) => {
      const validatedData = CreateGameSchema.parse(value) as CreateGame;
      validatedData.image = await uploadPoster(poster);
      await onClick(validatedData);
    },
  });

  const handleCancel = useCallback(() => {
    router.history.back();
  }, [router]);

  const handleSubmit = useCallback(
    (e: React.FormEvent) => {
      e.preventDefault();
      e.stopPropagation();
      form.handleSubmit();
    },
    [form]
  );

  return (
    <BgtPage>
      <BgtPageContent centered>
        <BgtCenteredCard title={title}>
          <form onSubmit={handleSubmit} className="w-full">
            <div className="flex flex-col gap-3 w-full">
              <div className="flex flex-row gap-3">
                <div className="flex-none">
                  <BgtImageSelector image={poster} setImage={setPoster} defaultImage={game?.image} />
                </div>
                <div className="grow">
                  <GameFormBasicFields form={form} disabled={disabled} currency={settings?.currency} />
                </div>
              </div>

              <GameFormPlayerFields form={form} disabled={disabled} />

              <GameFormTimeFields form={form} disabled={disabled} />

              {game === undefined && (
                <BgtFormField form={form} name="hasScoring" schema={CreateGameSchema.shape.hasScoring}>
                  {(field) => (
                    <BgtSwitch field={field} label={t('game.scoring.label')} className="pt-3" disabled={disabled} />
                  )}
                </BgtFormField>
              )}

              <div className="flex flex-row gap-2">
                <BgtButton
                  variant="cancel"
                  type="button"
                  className="flex-none"
                  onClick={handleCancel}
                  disabled={disabled}
                >
                  {t('common.cancel')}
                </BgtButton>
                <BgtButton type="submit" className="flex-1" variant="primary" disabled={disabled}>
                  {buttonText}
                </BgtButton>
              </div>
            </div>
          </form>
        </BgtCenteredCard>
      </BgtPageContent>
    </BgtPage>
  );
};
