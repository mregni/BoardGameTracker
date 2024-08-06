/* eslint-disable @typescript-eslint/no-misused-promises */
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { Button, Dialog } from '@radix-ui/themes';
import * as Form from '@radix-ui/react-form';
import { zodResolver } from '@hookform/resolvers/zod';

import { BgtSwitch } from '../BgtSwitch/BgtSwitch';
import { BgtHeading } from '../BgtHeading/BgtHeading';
import { BgtSelect } from '../BgtForm/BgtSelect';
import { BgtInputField } from '../BgtForm/BgtInputField';
import BgtButton from '../BgtButton/BgtButton';
import {
  CreateSessionPlayer,
  CreatePlayerSessionNoScoring,
  CreatePlayerSessionNoScoringSchema,
  CreatePlayerSessionSchema,
} from '../../models/Session/CreateSession';
import { usePlayers } from '../../hooks/usePlayers';
import { useLocations } from '../../hooks/useLocations';

interface Props {
  open: boolean;
  hasScoring: boolean;
  onClose: (player: CreateSessionPlayer | CreatePlayerSessionNoScoring) => void;
  onCancel: () => void;
  selectedPlayerIds: string[];
}

const CreatePlayerForm = (props: Props) => {
  const { open, hasScoring, onClose, selectedPlayerIds, onCancel } = props;
  const { t } = useTranslation();
  const { players } = usePlayers();
  const { locations } = useLocations();

  type PlayType<T extends boolean> = T extends true ? CreateSessionPlayer : CreatePlayerSessionNoScoring;
  type CreatePlayType = PlayType<typeof hasScoring>;

  const { handleSubmit, control } = useForm<CreatePlayType>({
    resolver: zodResolver(hasScoring ? CreatePlayerSessionSchema : CreatePlayerSessionNoScoringSchema),
    defaultValues: {
      firstPlay: false,
      won: false,
      score: 0,
    },
  });

  if (players === undefined || locations === undefined) return null;

  const onSubmit = (data: CreateSessionPlayer | CreatePlayerSessionNoScoring) => {
    onClose && onClose(data);
  };

  return (
    <Dialog.Root open={open}>
      <Dialog.Content className="bg-card-black">
        <BgtHeading size="6" className="uppercase">
          {t('player-session.new.title')}
        </BgtHeading>
        <Dialog.Description>{t('player-session.new.description')}</Dialog.Description>
        <form onSubmit={handleSubmit(onSubmit)}>
          <div className="flex flex-col gap-4 mt-3 mb-6">
            <BgtSelect
              control={control}
              label={t('player-session.new.player.label')}
              name="playerId"
              hasAvatars
              items={players
                .filter((player) => !selectedPlayerIds.includes(player.id.toString()))
                .map((value) => ({
                  label: value.name,
                  value: value.id.toString(),
                  image: value.image,
                }))}
            />
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
          <div className="flex justify-end gap-3">
            <Dialog.Close>
              <>
                <Form.Submit asChild>
                  <BgtButton type="submit" variant="soft" color="primary">
                    {t('player-session.new.save')}
                  </BgtButton>
                </Form.Submit>
                <BgtButton type="button" variant="soft" color="cancel" onClick={() => onCancel()}>
                  {t('common.cancel')}
                </BgtButton>
              </>
            </Dialog.Close>
          </div>
        </form>
      </Dialog.Content>
    </Dialog.Root>
  );
};

export const BgtCreatePlayerModal = (props: Props) => {
  return props.open && <CreatePlayerForm {...props} />;
};
