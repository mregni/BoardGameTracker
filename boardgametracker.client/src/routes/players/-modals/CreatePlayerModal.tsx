import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';

import { usePlayerModal } from '../-hooks/usePlayerModal';

import { useToasts } from '@/routes/-hooks/useToasts';
import { CreatePlayer, CreatePlayerSchema, Player } from '@/models';
import { BgtInputField } from '@/components/BgtForm/BgtInputField';
import { BgtImageSelector } from '@/components/BgtForm/BgtImageSelector';
import {
  BgtDialog,
  BgtDialogClose,
  BgtDialogContent,
  BgtDialogDescription,
  BgtDialogTitle,
} from '@/components/BgtDialog/BgtDialog';
import BgtButton from '@/components/BgtButton/BgtButton';

interface Props {
  open: boolean;
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
}

export const CreatePlayerModal = (props: Props) => {
  const { open, setOpen } = props;
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

  const { handleSubmit, control } = useForm<CreatePlayer>({
    resolver: zodResolver(CreatePlayerSchema),
    defaultValues: {
      name: '',
    },
  });

  const onSubmit = async (data: CreatePlayer) => {
    const player: Player = {
      id: '0',
      name: data.name,
      image: null,
      badges: [],
    };

    if (image !== undefined) {
      const savedImage = await uploadImage({ type: 0, file: image });
      player.image = savedImage ?? null;
    }

    await savePlayer(player);
    setOpen(false);
  };

  return (
    <BgtDialog open={open}>
      <BgtDialogContent>
        <BgtDialogTitle>{t('player.new.title')}</BgtDialogTitle>
        <BgtDialogDescription>{t('player.new.description')}</BgtDialogDescription>
        <form onSubmit={(event) => void handleSubmit(onSubmit)(event)}>
          <div className="flex flex-row gap-3 mt-3 mb-6">
            <div className="flex-none">
              <BgtImageSelector image={image} setImage={setImage} />
            </div>
            <div className="flex-grow">
              <BgtInputField
                type="text"
                placeholder={t('player.name.placeholder')}
                name="name"
                label={t('common.name')}
                control={control}
                disabled={isLoading}
              />
            </div>
          </div>
          <BgtDialogClose>
            <BgtButton variant="outline" onClick={() => setOpen(false)} disabled={isLoading}>
              {t('common.cancel')}
            </BgtButton>
            <BgtButton type="submit" variant="soft" disabled={isLoading}>
              {t('player.new.save')}
            </BgtButton>
          </BgtDialogClose>
        </form>
      </BgtDialogContent>
    </BgtDialog>
  );
};
