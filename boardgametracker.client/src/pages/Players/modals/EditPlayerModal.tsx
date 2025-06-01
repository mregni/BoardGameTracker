import { z } from 'zod';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';

import { usePlayerModal } from '../hooks/usePlayerModal';

import { useToast } from '@/providers/BgtToastProvider';
import { Player } from '@/models';
import { useImages } from '@/hooks/useImages';
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

interface FormProps {
  id: number;
  name: string;
}

export const EditPlayerModal = (props: Props) => {
  const { open, setOpen, player } = props;
  const { t } = useTranslation();
  const [image, setImage] = useState<File | undefined | null>(undefined);
  const { showInfoToast } = useToast();

  const { isPending, uploadPlayerImage } = useImages();

  const onUpdateSuccess = () => {
    showInfoToast('player.notifications.updated');
  };

  const { update, updateIsPending } = usePlayerModal({ onUpdateSuccess });

  const schema = z.object({
    name: z.string().min(1, { message: t('player.new.name.required') }),
  });

  const { handleSubmit, control } = useForm<FormProps>({
    resolver: zodResolver(schema),
    defaultValues: {
      name: player.name,
    },
  });

  const onSubmit = async (data: FormProps) => {
    player.name = data.name;

    if (image !== undefined && image !== null) {
      const savedImage = await uploadPlayerImage(image);
      player.image = savedImage ?? null;
    } else if (image === null) {
      player.image = null;
    }

    await update(player);
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
                disabled={isPending || updateIsPending}
              />
            </div>
          </div>
          <BgtDialogClose>
            <BgtButton variant="outline" onClick={() => setOpen(false)} disabled={isPending || updateIsPending}>
              {t('common.cancel')}
            </BgtButton>
            <BgtButton type="submit" variant="soft" disabled={isPending || updateIsPending}>
              {t('player.update.save')}
            </BgtButton>
          </BgtDialogClose>
        </form>
      </BgtDialogContent>
    </BgtDialog>
  );
};
