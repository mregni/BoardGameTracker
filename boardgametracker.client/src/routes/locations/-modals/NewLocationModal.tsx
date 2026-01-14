import { useTranslation } from 'react-i18next';
import { useForm } from '@tanstack/react-form';

import { useLocationModal } from '../-hooks/useLocationModal';

import { useToasts } from '@/routes/-hooks/useToasts';
import { CreateLocationSchema } from '@/models';
import { BgtInputField } from '@/components/BgtForm/BgtInputField';
import { BgtDialog, BgtDialogContent, BgtDialogTitle, BgtDialogClose } from '@/components/BgtDialog/BgtDialog';
import BgtButton from '@/components/BgtButton/BgtButton';

interface Props {
  open: boolean;
  close: () => void;
}

export const NewLocationModal = (props: Props) => {
  const { open, close } = props;
  const { successToast, errorToast } = useToasts();
  const { t } = useTranslation();

  const onSaveSuccess = () => {
    successToast('location.notifications.created');
    close();
  };
  const onSaveError = () => {
    errorToast('location.notifications.create-failed');
  };

  const { saveLocation, isLoading } = useLocationModal({ onSaveSuccess, onSaveError });

  const form = useForm({
    defaultValues: {
      name: '',
    },
    onSubmit: async ({ value }) => {
      const validatedData = CreateLocationSchema.parse(value);
      await saveLocation(validatedData);
    },
  });

  return (
    <BgtDialog open={open}>
      <BgtDialogContent>
        <form
          onSubmit={(e) => {
            e.preventDefault();
            e.stopPropagation();
            form.handleSubmit();
          }}
          className="w-full"
        >
          <BgtDialogTitle>{t('location.new.title')}</BgtDialogTitle>
          <div className="flex flex-col gap-2 mb-3">
            <form.Field
              name="name"
              validators={{
                onChange: ({ value }) => {
                  const result = CreateLocationSchema.shape.name.safeParse(value);
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
                  label={t('location.new.name.placeholder')}
                  disabled={isLoading}
                />
              )}
            </form.Field>
          </div>
          <BgtDialogClose>
            <BgtButton variant="cancel" onClick={() => close()} disabled={isLoading}>
              {t('common.cancel')}
            </BgtButton>
            <BgtButton variant="primary" type="submit" disabled={isLoading}>
              {t('common.save')}
            </BgtButton>
          </BgtDialogClose>
        </form>
      </BgtDialogContent>
    </BgtDialog>
  );
};
