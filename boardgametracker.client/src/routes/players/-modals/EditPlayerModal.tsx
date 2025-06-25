import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';

import { usePlayerModal } from '../-hooks/usePlayerModal';

import { useToasts } from '@/routes/-hooks/useToasts';
import { CreatePlayerSchema, Player, UpdatePlayer } from '@/models';
import { BgtInputField } from '@/components/BgtForm/BgtInputField';
import { BgtImageSelector } from '@/components/BgtForm/BgtImageSelector';
import {
  BgtDialog,
  BgtDialogContent,
  BgtDialogTitle,
  BgtDialogDescription,
  BgtDialogClose,
} from '@/components/BgtDialog/BgtDialog';
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

  const onUpdateSuccess = () => {
    successToast('player.notifications.updated');
  };

  const onUpdateError = () => {
    errorToast('player.notifications.update-failed');
  };

  const { updatePlayer, uploadImage, isLoading } = usePlayerModal({ onUpdateSuccess, onUpdateError });

  const { handleSubmit, control } = useForm<UpdatePlayer>({
    resolver: zodResolver(CreatePlayerSchema),
    defaultValues: {
      name: player.name,
    },
  });

  const onSubmit = async (data: UpdatePlayer) => {
    player.name = data.name;

    if (image !== undefined && image !== null) {
      const savedImage = await uploadImage({ type: 0, file: image });
      player.image = savedImage ?? null;
    } else if (image === null) {
      player.image = null;
    }

    await updatePlayer(player);
    setOpen(false);
  };

  return (
    <BgtDialog open={open}>
      <BgtDialogContent>
        <BgtDialogTitle>{t('player.update.title')}</BgtDialogTitle>
        <BgtDialogDescription>{t('player.update.description')}</BgtDialogDescription>
        <form onSubmit={(event) => void handleSubmit(onSubmit)(event)}>
          <div className="flex flex-row gap-3 mt-3 mb-6">
            <div className="flex-none">
              <BgtImageSelector image={image} setImage={setImage} defaultImage={player.image} />
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
              {t('player.update.save')}
            </BgtButton>
          </BgtDialogClose>
        </form>
      </BgtDialogContent>
    </BgtDialog>
  );
};
