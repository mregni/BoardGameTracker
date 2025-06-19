import * as z from 'zod';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';

import { usePlayerModal } from '../hooks/usePlayerModal';

import { Player } from '@/models';
import { useToasts } from '@/hooks/useToasts';
import { useImages } from '@/hooks/useImages';
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

interface FormProps {
  name: string;
}

export const CreatePlayerModal = (props: Props) => {
  const { open, setOpen } = props;
  const { t } = useTranslation();
  const [image, setImage] = useState<File | undefined>(undefined);
  const { successToast } = useToasts();

  const { isPending, uploadPlayerImage } = useImages();

  const onSuccess = () => {
    successToast('player.notifications.created');
  };

  const { save, isPending: playerIsPending } = usePlayerModal({ onSuccess });

  const schema = z.object({
    name: z.string().min(1, { message: t('player.new.name.required') }),
  });

  const { handleSubmit, control } = useForm<FormProps>({
    resolver: zodResolver(schema),
    defaultValues: {
      name: '',
    },
  });

  const onSubmit = async (data: FormProps) => {
    const player: Player = {
      id: '0',
      name: data.name,
      image: null,
      badges: [],
    };

    if (image !== undefined) {
      const savedImage = await uploadPlayerImage(image);
      player.image = savedImage ?? null;
    }

    await save(player);
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
                disabled={isPending || playerIsPending}
              />
            </div>
          </div>
          <BgtDialogClose>
            <BgtButton variant="outline" onClick={() => setOpen(false)} disabled={isPending || playerIsPending}>
              {t('common.cancel')}
            </BgtButton>
            <BgtButton type="submit" variant="soft" disabled={isPending || playerIsPending}>
              {t('player.new.save')}
            </BgtButton>
          </BgtDialogClose>
        </form>
      </BgtDialogContent>
    </BgtDialog>
  );
};
