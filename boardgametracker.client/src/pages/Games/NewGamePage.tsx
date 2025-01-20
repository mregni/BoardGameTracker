import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';

import { useGames } from './hooks/useGames';

import { getItemStateTranslationKeyByString } from '@/utils/ItemStateUtils';
import { useToast } from '@/providers/BgtToastProvider';
import { CreateGame, CreateGameSchema } from '@/models/Games/CreateGame';
import { Game, GameState } from '@/models';
import { useSettings } from '@/hooks/useSettings';
import { useImages } from '@/hooks/useImages';
import { BgtSwitch } from '@/components/BgtSwitch/BgtSwitch';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtTextArea } from '@/components/BgtForm/BgtTextArea';
import { BgtSelect } from '@/components/BgtForm/BgtSelect';
import { BgtInputField } from '@/components/BgtForm/BgtInputField';
import { BgtImageSelector } from '@/components/BgtForm/BgtImageSelector';
import { BgtCenteredCard } from '@/components/BgtCard/BgtCenteredCard';
import BgtButton from '@/components/BgtButton/BgtButton';

export const NewGamePage = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { settings } = useSettings();
  const [poster, setPoster] = useState<File | undefined>(undefined);
  const { uploadGameImage } = useImages();
  const { showInfoToast, showErrorToast } = useToast();

  const onSuccess = (game: Game) => {
    showInfoToast('game.notifications.created');
    navigate(`/games/${game.id}`);
    window.scrollTo(0, 0);
  };

  const onError = () => {
    showErrorToast('game.notifications.failed');
  };

  const { saveGame } = useGames({ onSuccess, onError });

  const { handleSubmit, control, register } = useForm<CreateGame>({
    resolver: zodResolver(CreateGameSchema),
    defaultValues: {
      title: '',
      hasScoring: true,
      description: '',
      state: 1,
      yearPublished: undefined,
      maxPlayers: 0,
      minPlayers: 0,
      maxPlayTime: 0,
      minPlayTime: 0,
      minAge: 0,
    },
  });

  const onSubmit = async (game: CreateGame) => {
    if (poster !== undefined) {
      const savedImage = await uploadGameImage(poster);
      game.image = savedImage ?? undefined;
    }

    await saveGame(game);
  };

  return (
    <BgtPage>
      <BgtPageContent>
        <BgtCenteredCard title={t('game.new.manual.title')}>
          <form onSubmit={(event) => void handleSubmit(onSubmit)(event)} className="w-full">
            <div className="flex flex-col gap-3 w-full">
              <div className="flex flex-row gap-3">
                <div className="flex-none">
                  <BgtImageSelector image={poster} setImage={setPoster} />
                </div>
                <div className="flex-grow">
                  <BgtInputField
                    name="title"
                    type="text"
                    control={control}
                    label={t('game.new.manual.game-title.label')}
                  />
                </div>
              </div>
              <BgtInputField
                valueAsNumber
                name="bggId"
                type="number"
                control={control}
                label={t('game.bgg.placeholder')}
              />
              <BgtInputField
                label={t('game.price.label')}
                name="buyingPrice"
                type="number"
                placeholder={t('game.price.placeholder')}
                control={control}
                prefixLabel={settings.data?.currency}
              />
              <BgtInputField
                label={t('game.added-date.label')}
                name="date"
                type="date"
                control={control}
                className="pr-2"
              />
              <BgtSelect
                control={control}
                label={t('game.state.label')}
                name="state"
                items={Object.keys(GameState)
                  .filter((value) => !isNaN(Number(value)))
                  .map((value) => ({ label: t(getItemStateTranslationKeyByString(value)), value: value, image: null }))}
              />
              <BgtInputField
                label={t('game.new.manual.year.label')}
                name="yearPublished"
                type="number"
                control={control}
                className="pr-2"
              />
              <div className="flex flex-row gap-2">
                <BgtInputField
                  label={t('game.new.manual.min-players.label')}
                  name="minPlayers"
                  type="number"
                  control={control}
                  className="pr-2"
                />
                <BgtInputField
                  label={t('game.new.manual.max-players.label')}
                  name="maxPlayers"
                  type="number"
                  control={control}
                  className="pr-2"
                />
              </div>
              <div className="flex flex-row gap-2">
                <BgtInputField
                  label={t('game.new.manual.min-time.label')}
                  name="minPlayTime"
                  type="number"
                  control={control}
                  className="pr-2"
                  suffixLabel={t('common.minutes_abbreviation')}
                />
                <BgtInputField
                  label={t('game.new.manual.max-time.label')}
                  name="maxPlayTime"
                  type="number"
                  control={control}
                  className="pr-2"
                  suffixLabel={t('common.minutes_abbreviation')}
                />
              </div>
              <BgtInputField
                label={t('game.new.manual.min-age.label')}
                name="minAge"
                type="number"
                control={control}
                className="pr-2"
              />

              <BgtSwitch label={t('game.scoring.label')} control={control} name="hasScoring" className="pt-3" />

              <BgtTextArea label={t('game.new.manual.description.label')} name="description" register={register} />

              <div className="flex flex-row gap-2">
                <BgtButton variant="outline" type="button" className="flex-none" onClick={() => navigate(-1)}>
                  {t('common.cancel')}
                </BgtButton>
                <BgtButton type="submit" className="flex-1" variant="soft">
                  {t('player-session.save')}
                </BgtButton>
              </div>
            </div>
          </form>
        </BgtCenteredCard>
      </BgtPageContent>
    </BgtPage>
  );
};
