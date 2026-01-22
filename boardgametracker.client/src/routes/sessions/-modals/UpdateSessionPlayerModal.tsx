import { useTranslation } from 'react-i18next';
import { useForm } from '@tanstack/react-form';
import { Dialog } from '@radix-ui/themes';

import { usePlayerById } from '@/routes/-hooks/usePlayerById';
import {
  CreateSessionPlayer,
  CreatePlayerSessionNoScoring,
  CreatePlayerSessionNoScoringSchema,
  CreatePlayerSessionSchema,
} from '@/models/';
import { BgtFormField, BgtSwitch, BgtInputField } from '@/components/BgtForm';
import { BgtDialog, BgtDialogClose, BgtDialogContent, BgtDialogTitle } from '@/components/BgtDialog';
import BgtButton from '@/components/BgtButton/BgtButton';

interface Props {
  open: boolean;
  hasScoring: boolean;
  onClose: (player: CreateSessionPlayer | CreatePlayerSessionNoScoring) => void;
  onCancel: () => void;
  selectedPlayerIds: number[];
  playerToEdit: CreateSessionPlayer | CreatePlayerSessionNoScoring | undefined;
}

const UpdateSessionPlayerForm = (props: Props) => {
  const { open, hasScoring, onClose, playerToEdit, onCancel } = props;
  const { t } = useTranslation();
  const { playerById } = usePlayerById();

  const schema = hasScoring ? CreatePlayerSessionSchema : CreatePlayerSessionNoScoringSchema;

  const form = useForm({
    defaultValues: {
      firstPlay: playerToEdit?.firstPlay ?? false,
      won: playerToEdit?.won ?? false,
      score: playerToEdit !== undefined && 'score' in playerToEdit ? playerToEdit?.score : 0,
      playerId: playerToEdit?.playerId ?? '',
    },
    onSubmit: async ({ value }) => {
      const validatedData = schema.parse(value);
      onClose(validatedData);
    },
  });

  return (
    <BgtDialog open={open}>
      <BgtDialogContent>
        <BgtDialogTitle>{t('player-session.update.title')}</BgtDialogTitle>
        <Dialog.Description>
          {t('player-session.update.description', { name: playerById(playerToEdit?.playerId)?.name })}
        </Dialog.Description>
        <form
          onSubmit={(e) => {
            e.preventDefault();
            e.stopPropagation();
            form.handleSubmit();
          }}
        >
          <div className="flex flex-col gap-4 mt-3 mb-6">
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
              {t('player-session.update.save')}
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
