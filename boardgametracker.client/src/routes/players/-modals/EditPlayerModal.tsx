import { useTranslation } from 'react-i18next';
import { useCallback, useState } from 'react';
import { useForm } from '@tanstack/react-form';

import { usePlayerModal } from '../-hooks/usePlayerModal';

import { useToasts } from '@/routes/-hooks/useToasts';
import { CreatePlayerSchema, Player } from '@/models';
import { BgtInputField, BgtImageSelector } from '@/components/BgtForm';
import {
  BgtDialog,
  BgtDialogContent,
  BgtDialogTitle,
  BgtDialogDescription,
  BgtDialogClose,
} from '@/components/BgtDialog';
import BgtButton from '@/components/BgtButton/BgtButton';

interface Props {
  open: boolean;
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
  player: Player;
}

export const EditPlayerModal = (props: Props) => {
  const { open, setOpen, player } = props;
  const { t } = useTranslation();
  const [image, setImage] = useState<File | undefined | null>(undefined);
  const { successToast, errorToast } = useToasts();

  const onUpdateSuccess = useCallback(() => {
    successToast('player.notifications.updated');
  }, [successToast]);

  const onUpdateError = useCallback(() => {
    errorToast('player.notifications.update-failed');
  }, [errorToast]);

  const { updatePlayer, uploadImage, isLoading } = usePlayerModal({ onUpdateSuccess, onUpdateError });

  const handleClose = useCallback(() => {
    setOpen(false);
  }, [setOpen]);

  const form = useForm({
    defaultValues: {
      name: player.name,
    },
    onSubmit: async ({ value }) => {
      const validatedData = CreatePlayerSchema.parse(value);

      const updatedPlayer: Player = {
        ...player,
        name: validatedData.name,
      };

      if (image !== undefined && image !== null) {
        const savedImage = await uploadImage({ type: 0, file: image });
        updatedPlayer.image = savedImage ?? null;
      } else if (image === null) {
        updatedPlayer.image = null;
      }

      await updatePlayer(updatedPlayer);
      setOpen(false);
    },
  });

  return (
    <BgtDialog open={open}>
      <BgtDialogContent>
        <BgtDialogTitle>{t('player.update.title')}</BgtDialogTitle>
        <BgtDialogDescription>{t('player.update.description')}</BgtDialogDescription>
        <form
          onSubmit={(e) => {
            e.preventDefault();
            e.stopPropagation();
            form.handleSubmit();
          }}
        >
          <div className="flex flex-row gap-3 mt-3 mb-6">
            <div className="flex-none">
              <BgtImageSelector image={image} setImage={setImage} defaultImage={player.image} />
            </div>
            <div className="grow">
              <form.Field
                name="name"
                validators={{
                  onChange: ({ value }) => {
                    const result = CreatePlayerSchema.shape.name.safeParse(value);
                    if (!result.success) {
                      return t(result.error.errors[0].message);
                    }
                    return undefined;
                  },
                }}
              >
                {(field) => (
                  <BgtInputField
                    field={field}
                    type="text"
                    placeholder={t('player.name.placeholder')}
                    label={t('common.name')}
                    disabled={isLoading}
                  />
                )}
              </form.Field>
            </div>
          </div>
          <BgtDialogClose>
            <BgtButton variant="cancel" onClick={handleClose} disabled={isLoading}>
              {t('common.cancel')}
            </BgtButton>
            <BgtButton type="submit" variant="primary" disabled={isLoading}>
              {t('player.update.save')}
            </BgtButton>
          </BgtDialogClose>
        </form>
      </BgtDialogContent>
    </BgtDialog>
  );
};
