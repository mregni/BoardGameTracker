import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import * as Form from '@radix-ui/react-form';
import { zodResolver } from '@hookform/resolvers/zod';

import { usePlayers } from '../../Players/hooks/usePlayers';

import {
  CreateSessionPlayer,
  CreatePlayerSessionNoScoring,
  CreatePlayerSessionNoScoringSchema,
  CreatePlayerSessionSchema,
} from '@/models/Session/CreateSession';
import { useLocations } from '@/hooks/useLocations';
import { BgtSwitch } from '@/components/BgtSwitch/BgtSwitch';
import { BgtSelect } from '@/components/BgtForm/BgtSelect';
import { BgtInputField } from '@/components/BgtForm/BgtInputField';
import {
  BgtDialog,
  BgtDialogContent,
  BgtDialogDescription,
  BgtDialogClose,
  BgtDialogTitle,
} from '@/components/BgtDialog/BgtDialog';
import BgtButton from '@/components/BgtButton/BgtButton';

interface Props {
  open: boolean;
  hasScoring: boolean;
  onClose: (player: CreateSessionPlayer | CreatePlayerSessionNoScoring) => void;
  onCancel: () => void;
  selectedPlayerIds: string[];
}

const CreateSessionPlayerForm = (props: Props) => {
  const { open, hasScoring, onClose, selectedPlayerIds, onCancel } = props;
  const { t } = useTranslation();
  const { players } = usePlayers({});
  const { locations } = useLocations({});

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

  if (locations === undefined) return null;

  const onSubmit = (data: CreateSessionPlayer | CreatePlayerSessionNoScoring) => {
    onClose && onClose(data);
  };

  return (
    <BgtDialog open={open}>
      <BgtDialogContent>
        <BgtDialogTitle>{t('player-session.new.title')}</BgtDialogTitle>
        <BgtDialogDescription>{t('player-session.new.description')}</BgtDialogDescription>
        <form onSubmit={handleSubmit(onSubmit)}>
          <div className="flex flex-col gap-4 mt-3 mb-6">
            <BgtSelect
              control={control}
              label={t('player-session.new.player.label')}
              name="playerId"
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
          <BgtDialogClose>
            <BgtButton type="button" variant="soft" color="cancel" onClick={() => onCancel()}>
              {t('common.cancel')}
            </BgtButton>
            <Form.Submit asChild>
              <BgtButton type="submit" variant="soft" color="primary">
                {t('player-session.new.save')}
              </BgtButton>
            </Form.Submit>
          </BgtDialogClose>
        </form>
      </BgtDialogContent>
    </BgtDialog>
  );
};

export const CreateSessionPlayerModal = (props: Props) => {
  return props.open && <CreateSessionPlayerForm {...props} />;
};
