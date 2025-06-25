import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { useState } from 'react';
import { useRouter } from '@tanstack/react-router';
import { zodResolver } from '@hookform/resolvers/zod';

import { useGameForm } from '../-hooks/useGameForm';

import { getItemStateTranslationKeyByString } from '@/utils/ItemStateUtils';
import { CreateGame, CreateGameSchema } from '@/models/Games/CreateGame';
import { Game, GameState } from '@/models';
import { BgtSwitch } from '@/components/BgtSwitch/BgtSwitch';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtTextArea } from '@/components/BgtForm/BgtTextArea';
import { BgtSelect } from '@/components/BgtForm/BgtSelect';
import { BgtInputField } from '@/components/BgtForm/BgtInputField';
import { BgtImageSelector } from '@/components/BgtForm/BgtImageSelector';
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
  const { settings, uploadImage } = useGameForm();
  const { t } = useTranslation();
  const [poster, setPoster] = useState<File | undefined | null>(undefined);
  const router = useRouter();

  const { handleSubmit, control, register } = useForm<CreateGame>({
    resolver: zodResolver(CreateGameSchema),
    defaultValues: {
      title: game?.title ?? '',
      hasScoring: game?.hasScoring ?? true,
      description: game?.description ?? '',
      state: game?.state ?? GameState.Owned,
      yearPublished: game?.yearPublished ?? undefined,
      maxPlayers: game?.maxPlayers ?? undefined,
      minPlayers: game?.minPlayers ?? undefined,
      maxPlayTime: game?.maxPlayTime ?? undefined,
      minPlayTime: game?.minPlayTime ?? undefined,
      minAge: game?.minAge ?? undefined,
      bggId: game?.bggId ?? undefined,
      buyingPrice: game?.buyingPrice ?? 0,
      additionDate: game?.additionDate ?? new Date(),
      image: game?.image ?? null,
    },
  });

  const onSubmit = async (game: CreateGame) => {
    if (poster !== undefined && poster !== null) {
      const savedImage = await uploadImage({ type: 1, file: poster });
      game.image = savedImage ?? null;
    } else if (poster === null) {
      game.image = null;
    }

    await onClick(game);
  };

  return (
    <BgtPage>
      <BgtPageContent>
        <BgtCenteredCard title={title}>
          <form onSubmit={(event) => void handleSubmit(onSubmit)(event)} className="w-full">
            <div className="flex flex-col gap-3 w-full">
              <div className="flex flex-row gap-3">
                <div className="flex-none">
                  <BgtImageSelector image={poster} setImage={setPoster} defaultImage={game?.image} />
                </div>
                <div className="flex-grow">
                  <BgtInputField
                    name="title"
                    type="text"
                    control={control}
                    disabled={disabled}
                    label={t('game.new.manual.game-title.label')}
                  />
                </div>
              </div>
              <BgtInputField
                valueAsNumber
                name="bggId"
                type="number"
                control={control}
                disabled={disabled}
                label={t('game.bgg.placeholder')}
              />
              <BgtInputField
                label={t('game.price.label')}
                name="buyingPrice"
                type="number"
                placeholder={t('game.price.placeholder')}
                control={control}
                disabled={disabled}
                prefixLabel={settings?.currency}
              />
              <BgtInputField
                label={t('game.added-date.label')}
                name="additionDate"
                type="date"
                control={control}
                disabled={disabled}
                className="pr-2"
              />
              <BgtSelect
                control={control}
                label={t('game.state.label')}
                name="state"
                disabled={disabled}
                items={Object.keys(GameState)
                  .filter((value) => !isNaN(Number(value)))
                  .map((value) => ({ label: t(getItemStateTranslationKeyByString(value)), value: value }))}
              />
              <BgtInputField
                label={t('game.new.manual.year.label')}
                name="yearPublished"
                type="number"
                disabled={disabled}
                control={control}
                className="pr-2"
              />
              <div className="flex flex-row gap-2">
                <BgtInputField
                  label={t('game.new.manual.min-players.label')}
                  name="minPlayers"
                  type="number"
                  disabled={disabled}
                  control={control}
                  className="pr-2"
                />
                <BgtInputField
                  label={t('game.new.manual.max-players.label')}
                  name="maxPlayers"
                  type="number"
                  disabled={disabled}
                  control={control}
                  className="pr-2"
                />
              </div>
              <div className="flex flex-row gap-2">
                <BgtInputField
                  label={t('game.new.manual.min-time.label')}
                  name="minPlayTime"
                  type="number"
                  disabled={disabled}
                  control={control}
                  className="pr-2"
                  suffixLabel={t('common.minutes-abbreviation')}
                />
                <BgtInputField
                  label={t('game.new.manual.max-time.label')}
                  name="maxPlayTime"
                  type="number"
                  disabled={disabled}
                  control={control}
                  className="pr-2"
                  suffixLabel={t('common.minutes-abbreviation')}
                />
              </div>
              <BgtInputField
                label={t('game.new.manual.min-age.label')}
                name="minAge"
                type="number"
                disabled={disabled}
                control={control}
                className="pr-2"
              />

              {game === undefined && (
                <BgtSwitch
                  label={t('game.scoring.label')}
                  control={control}
                  name="hasScoring"
                  className="pt-3"
                  disabled={disabled}
                />
              )}

              <BgtTextArea
                label={t('game.new.manual.description.label')}
                name="description"
                register={register}
                disabled={disabled}
              />

              <div className="flex flex-row gap-2">
                <BgtButton
                  variant="outline"
                  type="button"
                  className="flex-none"
                  onClick={() => router.history.back()}
                  disabled={disabled}
                >
                  {t('common.cancel')}
                </BgtButton>
                <BgtButton type="submit" className="flex-1" variant="soft" disabled={disabled}>
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
