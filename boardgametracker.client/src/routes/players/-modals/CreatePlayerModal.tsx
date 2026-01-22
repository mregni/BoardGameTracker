import { Bars } from 'react-loading-icons';
import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import { useForm } from '@tanstack/react-form';

import { usePlayerModal } from '../-hooks/usePlayerModal';

import { useToasts } from '@/routes/-hooks/useToasts';
import { CreatePlayerSchema, Player } from '@/models';
import { BgtInputField, BgtImageSelector } from '@/components/BgtForm';
import {
  BgtDialog,
  BgtDialogClose,
  BgtDialogContent,
  BgtDialogDescription,
  BgtDialogTitle,
} from '@/components/BgtDialog';
import BgtButton from '@/components/BgtButton/BgtButton';

interface Props {
  open: boolean;
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
  onPlayerCreated?: (player: Player) => void;
}

export const CreatePlayerModal = (props: Props) => {
  const { open, setOpen, onPlayerCreated } = props;
  const { t } = useTranslation();
  const [image, setImage] = useState<File | undefined | null>(undefined);
  const { successToast, errorToast } = useToasts();

  const onSaveSuccess = () => {
    successToast('player.notifications.created');
  };

  const onSaveError = () => {
    errorToast('player.notifications.create-failed');
  };

  const { savePlayer, uploadImage, isLoading } = usePlayerModal({ onSaveSuccess, onSaveError });

  const form = useForm({
    defaultValues: {
      name: '',
    },
    onSubmit: async ({ value }) => {
      const validatedData = CreatePlayerSchema.parse(value);

      const player: Player = {
        id: 0,
        name: validatedData.name,
        image: null,
        badges: [],
      };

      if (image !== undefined) {
        const savedImage = await uploadImage({ type: 0, file: image });
        player.image = savedImage ?? null;
      }

      const savedPlayer = await savePlayer(player);

      // Call the callback if provided
      if (onPlayerCreated) {
        onPlayerCreated(savedPlayer);
      }

      // Reset form and image
      form.reset();
      setImage(undefined);
      setOpen(false);
    },
  });

  const handleCancel = () => {
    // Reset form and image when canceling
    form.reset();
    setImage(undefined);
    setOpen(false);
  };

  return (
    <BgtDialog open={open}>
      <BgtDialogContent>
        <BgtDialogTitle>{t('player.new.title')}</BgtDialogTitle>
        <BgtDialogDescription>{t('player.new.description')}</BgtDialogDescription>
        <form
          onSubmit={(e) => {
            e.preventDefault();
            e.stopPropagation();
            form.handleSubmit();
          }}
        >
          <div className="flex flex-row gap-3 mt-3 mb-6">
            <div className="flex-none">
              <BgtImageSelector image={image} setImage={setImage} />
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
            <BgtButton variant="cancel" onClick={handleCancel} disabled={isLoading}>
              {t('common.cancel')}
            </BgtButton>
            <BgtButton type="submit" variant="primary" disabled={isLoading} className="flex-1">
              {isLoading && <Bars className="size-4" />}
              {t('player.new.save')}
            </BgtButton>
          </BgtDialogClose>
        </form>
      </BgtDialogContent>
    </BgtDialog>
  );
};
