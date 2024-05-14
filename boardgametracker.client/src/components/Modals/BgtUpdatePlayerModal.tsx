/* eslint-disable @typescript-eslint/no-misused-promises */
import { useTranslation } from 'react-i18next';
import { FieldArrayWithId, useForm } from 'react-hook-form';
import { Button, Dialog } from '@radix-ui/themes';
import * as Form from '@radix-ui/react-form';
import { zodResolver } from '@hookform/resolvers/zod';

import { BgtSwitch } from '../BgtSwitch/BgtSwitch';
import { BgtInputField } from '../BgtForm/BgtInputField';
import {
  CreatePlay,
  CreatePlayPlayer,
  CreatePlayPlayerNoScoring,
  CreatePlayPlayerNoScoringSchema,
  CreatePlayPlayerSchema,
} from '../../models/Plays/CreatePlay';
import { usePlayers } from '../../hooks/usePlayers';
import { useLocations } from '../../hooks/useLocations';

interface Props {
  open: boolean;
  hasScoring: boolean;
  onClose: (player: CreatePlayPlayer | CreatePlayPlayerNoScoring) => void;
  onCancel: () => void;
  selectedPlayerIds: string[];
  playerToEdit: FieldArrayWithId<CreatePlay> | undefined;
}

const UpdatePlayerForm = (props: Props) => {
  const { open, hasScoring, onClose, playerToEdit, onCancel } = props;
  const { t } = useTranslation();
  const { players } = usePlayers();
  const { locations } = useLocations();
  const { byId } = usePlayers();

  type PlayType<T extends boolean> = T extends true ? CreatePlayPlayer : CreatePlayPlayerNoScoring;
  type CreatePlayType = PlayType<typeof hasScoring>;

  const { handleSubmit, control } = useForm<CreatePlayType>({
    resolver: zodResolver(hasScoring ? CreatePlayPlayerSchema : CreatePlayPlayerNoScoringSchema),
    defaultValues: {
      firstPlay: playerToEdit?.firstPlay,
      won: playerToEdit?.won,
      isBot: playerToEdit?.isBot,
      score: playerToEdit !== undefined && 'score' in playerToEdit ? playerToEdit?.score : undefined,
      playerId: playerToEdit?.playerId,
    },
  });

  if (players === undefined || locations === undefined) return null;

  const onSubmit = (data: CreatePlayPlayer | CreatePlayPlayerNoScoring) => {
    onClose && onClose(data);
  };

  return (
    <Dialog.Root open={open}>
      <Dialog.Content>
        <Dialog.Title>{t('playplayer.update.title')}</Dialog.Title>
        <Dialog.Description>{t('playplayer.update.description', { name: byId(playerToEdit?.playerId)?.name })}</Dialog.Description>
        <form onSubmit={handleSubmit(onSubmit)}>
          <div className="flex flex-col gap-4 mt-3 mb-6">
            {hasScoring && <BgtInputField name="score" type="number" valueAsNumber control={control} label={t('playplayer.score.label')} />}
            <BgtSwitch label={t('playplayer.won.label')} control={control} name="won" className="mt-2" />
            <BgtSwitch label={t('playplayer.first-play.label')} control={control} name="firstPlay" className="mt-2" />
            <BgtSwitch label={t('playplayer.bot.label')} control={control} name="isBot" className="mt-2" />
          </div>
          <div className="flex justify-end gap-3">
            <Dialog.Close>
              <>
                <Form.Submit asChild>
                  <Button type="submit" variant="surface" color="orange">
                    {t('playplayer.update.save')}
                  </Button>
                </Form.Submit>
                <Button type="button" variant="surface" color="gray" onClick={() => onCancel()}>
                  {t('common.cancel')}
                </Button>
              </>
            </Dialog.Close>
          </div>
        </form>
      </Dialog.Content>
    </Dialog.Root>
  );
};

export const BgtUpdatePlayerModal = (props: Props) => {
  return props.open && props.playerToEdit && <UpdatePlayerForm {...props} />;
};
