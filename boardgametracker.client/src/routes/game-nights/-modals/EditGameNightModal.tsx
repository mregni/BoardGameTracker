import { Bars } from 'react-loading-icons';
import { useTranslation } from 'react-i18next';

import { GameNightForm, GameNightFormValues } from '../-components/GameNightForm';

import { GameNight, Game, Location, Player } from '@/models';
import {
  BgtDialog,
  BgtDialogContent,
  BgtDialogDescription,
  BgtDialogClose,
  BgtDialogTitle,
} from '@/components/BgtDialog';
import BgtButton from '@/components/BgtButton/BgtButton';

interface Props {
  open: boolean;
  setOpen: (open: boolean) => void;
  gameNight: GameNight | null;
  players: Player[];
  games: Game[];
  locations: Location[];
  isLoading: boolean;
  onSave: (gameNight: GameNight) => Promise<unknown>;
}

export const EditGameNightModal = (props: Props) => {
  const { open, setOpen, gameNight, players, games, locations, isLoading, onSave } = props;
  const { t } = useTranslation();

  if (!gameNight) return null;

  const handleSubmit = async (values: GameNightFormValues) => {
    await onSave({
      ...gameNight,
      title: values.title,
      notes: values.notes,
      startDate: values.startDate,
      hostId: values.hostId,
      locationId: values.locationId,
      suggestedGames: games.filter((g) => values.suggestedGameIds.includes(g.id)),
      invitedPlayers: gameNight.invitedPlayers,
    });
  };

  const handleClose = () => {
    setOpen(false);
  };

  return (
    <BgtDialog open={open} onClose={handleClose}>
      <BgtDialogContent className="max-w-2xl!">
        <GameNightForm
          defaultValues={{
            title: gameNight.title,
            startDate: new Date(gameNight.startDate),
            locationId: gameNight.locationId,
            hostId: gameNight.hostId,
            notes: gameNight.notes,
            suggestedGameIds: gameNight.suggestedGames.map((g) => g.id),
            invitedPlayerIds: gameNight.invitedPlayers
              .filter((p) => p.playerId !== gameNight.hostId)
              .map((p) => p.playerId),
          }}
          players={players}
          games={games}
          locations={locations}
          isLoading={isLoading}
          onSubmit={handleSubmit}
          onClose={handleClose}
        >
          <BgtDialogTitle>{t('game-nights.edit.title')}</BgtDialogTitle>
          <BgtDialogDescription>{t('game-nights.edit.description')}</BgtDialogDescription>
        </GameNightForm>
        <BgtDialogClose>
          <BgtButton disabled={isLoading} variant="cancel" className="flex-1" onClick={handleClose}>
            {t('common.cancel')}
          </BgtButton>
          <BgtButton type="submit" form="game-night-form" disabled={isLoading} className="flex-1" variant="primary">
            {isLoading && <Bars className="size-4" />}
            {t('game-nights.edit.save')}
          </BgtButton>
        </BgtDialogClose>
      </BgtDialogContent>
    </BgtDialog>
  );
};
