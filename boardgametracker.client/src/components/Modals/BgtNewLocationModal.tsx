/* eslint-disable @typescript-eslint/no-misused-promises */
import { z } from 'zod';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { Button, Dialog } from '@radix-ui/themes';
import * as Form from '@radix-ui/react-form';
import { zodResolver } from '@hookform/resolvers/zod';

import { BgtInputField } from '../BgtForm/BgtInputField';
import { Location } from '../../models';
import { useLocations } from '../../hooks/useLocations';

interface Props {
  open: boolean;
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
  saved?: (locationId: number) => void;
}

interface FormProps {
  name: string;
}

export const BgtNewLocationModal = (props: Props) => {
  const { open, setOpen, saved } = props;
  const { t } = useTranslation();
  const { save, isSaving } = useLocations();

  const schema = z.object({
    name: z.string().min(1, { message: t('location.new.name.required') }),
  });

  const { register, handleSubmit, control, reset, trigger } = useForm<FormProps>({
    resolver: zodResolver(schema),
    mode: 'onSubmit',
  });

  const onSubmit = async (data: FormProps) => {
    const location: Location = {
      id: 0,
      name: data.name,
      playCount: 0,
    };

    const result = await save(location);
    reset();
    saved && saved(result.model.id);
    setOpen(false);
  };

  if (!open) return null;

  return (
    <Dialog.Root open={open}>
      <Dialog.Content>
        <Dialog.Title>{t('location.new.title')}</Dialog.Title>
        <Dialog.Description>{t('location.new.description')}</Dialog.Description>
        <form onSubmit={handleSubmit(onSubmit, onError)}>
          <div className="flex flex-col gap-3 mt-3">
            <div className="flex flex-col gap-4 mt-3 mb-3">
              <BgtInputField
                type="text"
                placeholder={t('location.new.name.placeholder')}
                name="name"
                label={t('common.name')}
                register={register}
                control={control}
              />
            </div>
            <div className="flex justify-end gap-3">
              <Dialog.Close>
                <>
                  <Form.Submit asChild>
                    <Button type="button" variant="surface" color="orange" disabled={isSaving} onClick={() => trigger()}>
                      {t('location.new.save')}
                    </Button>
                  </Form.Submit>
                  <Button variant="surface" color="gray" onClick={() => setOpen(false)} disabled={isSaving}>
                    {t('common.cancel')}
                  </Button>
                </>
              </Dialog.Close>
            </div>
          </div>
        </form>
      </Dialog.Content>
    </Dialog.Root>
  );
};
