import { useTranslation } from 'react-i18next';
import { FieldArrayWithId, useForm } from 'react-hook-form';
import { Dialog } from '@radix-ui/themes';
import * as Form from '@radix-ui/react-form';
import { zodResolver } from '@hookform/resolvers/zod';

import { usePlayers } from '../../Players/hooks/usePlayers';

import {
  CreateSession,
  CreateSessionPlayer,
  CreatePlayerSessionNoScoring,
  CreatePlayerSessionNoScoringSchema,
  CreatePlayerSessionSchema,
} from '@/models/Session/CreateSession';
import { usePlayerById } from '@/hooks/usePlayerById';
import { useLocations } from '@/hooks/useLocations';
import { BgtSwitch } from '@/components/BgtSwitch/BgtSwitch';
import { BgtInputField } from '@/components/BgtForm/BgtInputField';
import { BgtDialog, BgtDialogClose, BgtDialogContent, BgtDialogTitle } from '@/components/BgtDialog/BgtDialog';
import BgtButton from '@/components/BgtButton/BgtButton';

interface Props {
  open: boolean;
  hasScoring: boolean;
  onClose: (player: CreateSessionPlayer | CreatePlayerSessionNoScoring) => void;
  onCancel: () => void;
  selectedPlayerIds: string[];
  playerToEdit: FieldArrayWithId<CreateSession> | undefined;
}

const UpdateSessionPlayerForm = (props: Props) => {
  const { open, hasScoring, onClose, playerToEdit, onCancel } = props;
  const { t } = useTranslation();
  const { locations } = useLocations({});
  const { playerById } = usePlayerById();

  type PlayType<T extends boolean> = T extends true ? CreateSessionPlayer : CreatePlayerSessionNoScoring;
  type CreatePlayType = PlayType<typeof hasScoring>;

  const { handleSubmit, control } = useForm<CreatePlayType>({
    resolver: zodResolver(hasScoring ? CreatePlayerSessionSchema : CreatePlayerSessionNoScoringSchema),
    defaultValues: {
      firstPlay: playerToEdit?.firstPlay,
      won: playerToEdit?.won,
      score: playerToEdit !== undefined && 'score' in playerToEdit ? playerToEdit?.score : undefined,
      playerId: playerToEdit?.playerId,
    },
  });

  if (locations === undefined) return null;

  const onSubmit = (data: CreateSessionPlayer | CreatePlayerSessionNoScoring) => {
    onClose && onClose(data);
  };

  return (
    <BgtDialog open={open}>
      <BgtDialogContent>
        <BgtDialogTitle>{t('player-session.update.title')}</BgtDialogTitle>
        <Dialog.Description>
          {t('player-session.update.description', { name: playerById(playerToEdit?.playerId)?.name })}
        </Dialog.Description>
        <form onSubmit={handleSubmit(onSubmit)}>
          <div className="flex flex-col gap-4 mt-3 mb-6">
            {hasScoring && (
              <BgtInputField
                name="score"
                type="number"
                valueAsNumber
                control={control}
                label={t('player-session.score.label')}
              />
            )}
            <BgtSwitch label={t('player-session.won.label')} control={control} name="won" className="mt-2" />
            <BgtSwitch
              label={t('player-session.first-play.label')}
              control={control}
              name="firstPlay"
              className="mt-2"
            />
          </div>
          <BgtDialogClose>
            <Form.Submit asChild>
              <BgtButton type="submit" variant="soft" color="primary">
                {t('player-session.update.save')}
              </BgtButton>
            </Form.Submit>
            <BgtButton type="button" variant="soft" color="cancel" onClick={() => onCancel()}>
              {t('common.cancel')}
            </BgtButton>
          </BgtDialogClose>
        </form>
      </BgtDialogContent>
    </BgtDialog>
  );
};

export const UpdateSessionPlayerModal = (props: Props) => {
  return props.open && props.playerToEdit && <UpdateSessionPlayerForm {...props} />;
};
