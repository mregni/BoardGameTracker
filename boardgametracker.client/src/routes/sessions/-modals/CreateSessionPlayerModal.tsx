import { useTranslation } from 'react-i18next';
import { useState, useEffect } from 'react';
import { useForm } from '@tanstack/react-form';

import { CreatePlayerModal } from '@/routes/players/-modals/CreatePlayerModal';
import {
  CreateSessionPlayer,
  CreatePlayerSessionNoScoring,
  CreatePlayerSessionNoScoringSchema,
  CreatePlayerSessionSchema,
  Player,
} from '@/models';
import { BgtFormField, BgtSwitch, BgtSelect, BgtInputField } from '@/components/BgtForm';
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
  hasScoring: boolean;
  onClose: (player: CreateSessionPlayer | CreatePlayerSessionNoScoring) => void;
  onCancel: () => void;
  selectedPlayerIds: number[];
  players: Player[];
}

const CreateSessionPlayerForm = (props: Props) => {
  const { open, hasScoring, onClose, selectedPlayerIds, onCancel, players } = props;
  const { t } = useTranslation();

  const [openCreatePlayerModal, setOpenCreatePlayerModal] = useState(false);
  const [newlyCreatedPlayerId, setNewlyCreatedPlayerId] = useState<number | null>(null);

  const schema = hasScoring ? CreatePlayerSessionSchema : CreatePlayerSessionNoScoringSchema;

  const form = useForm({
    defaultValues: {
      playerId: '',
      firstPlay: false,
      won: false,
      score: 0,
    },
    onSubmit: async ({ value }) => {
      const validatedData = schema.parse(value);
      onClose(validatedData);
    },
  });

  const handlePlayerCreated = (player: Player) => {
    setNewlyCreatedPlayerId(player.id);
  };

  useEffect(() => {
    if (newlyCreatedPlayerId !== null) {
      const playerExists = players.some((p) => p.id === newlyCreatedPlayerId);
      if (playerExists) {
        form.setFieldValue('playerId', String(newlyCreatedPlayerId));
        setTimeout(() => setNewlyCreatedPlayerId(null), 0);
      }
    }
  }, [players, newlyCreatedPlayerId, form]);

  return (
    <BgtDialog open={open}>
      <BgtDialogContent>
        <BgtDialogTitle>{t('player-session.new.title')}</BgtDialogTitle>
        <BgtDialogDescription>{t('player-session.new.description')}</BgtDialogDescription>
        <form
          onSubmit={(e) => {
            e.preventDefault();
            e.stopPropagation();
            form.handleSubmit();
          }}
        >
          <div className="flex flex-col gap-4 mt-3 mb-6">
            <BgtFormField form={form} name="playerId" schema={schema.shape.playerId}>
              {(field) => (
                <BgtSelect
                  field={field}
                  label={t('player-session.new.player.label')}
                  items={players
                    .filter((player) => !selectedPlayerIds.includes(player.id))
                    .map((value) => ({
                      label: value.name,
                      value: value.id,
                      image: value.image,
                    }))}
                />
              )}
            </BgtFormField>
            <div>
              <BgtButton type="button" variant="text" size="1" onClick={() => setOpenCreatePlayerModal(true)}>
                + {t('player-session.new.create-player')}
              </BgtButton>
            </div>
            {hasScoring && (
              <BgtFormField form={form} name="score" schema={CreatePlayerSessionSchema.shape.score}>
                {(field) => <BgtInputField field={field} type="number" label={t('player-session.score.label')} />}
              </BgtFormField>
            )}
            <BgtFormField form={form} name="won" schema={schema.shape.won}>
              {(field) => <BgtSwitch field={field} label={t('player-session.won.label')} className="mt-2" />}
            </BgtFormField>
            <BgtFormField form={form} name="firstPlay" schema={schema.shape.firstPlay}>
              {(field) => <BgtSwitch field={field} label={t('player-session.first-play.label')} className="mt-2" />}
            </BgtFormField>
          </div>
          <BgtDialogClose>
            <BgtButton type="button" variant="cancel" onClick={() => onCancel()}>
              {t('common.cancel')}
            </BgtButton>
            <BgtButton type="submit" variant="primary">
              {t('player-session.new.save')}
            </BgtButton>
          </BgtDialogClose>
        </form>
      </BgtDialogContent>

      <CreatePlayerModal
        open={openCreatePlayerModal}
        setOpen={setOpenCreatePlayerModal}
        onPlayerCreated={handlePlayerCreated}
      />
    </BgtDialog>
  );
};

export const CreateSessionPlayerModal = (props: Props) => {
  return props.open && <CreateSessionPlayerForm {...props} />;
};
