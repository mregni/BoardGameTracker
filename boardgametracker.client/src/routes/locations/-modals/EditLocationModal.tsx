import { useTranslation } from 'react-i18next';
import { useForm } from '@tanstack/react-form';

import { useLocationModal } from '../-hooks/useLocationModal';

import { handleFormSubmit } from '@/utils/formUtils';
import { CreateLocationSchema, Location } from '@/models';
import { BgtInputField } from '@/components/BgtForm';
import { BgtDialog, BgtDialogClose, BgtDialogContent, BgtDialogTitle } from '@/components/BgtDialog';
import BgtButton from '@/components/BgtButton/BgtButton';

interface Props {
  location: Location;
  open: boolean;
  close: () => void;
}

export const EditLocationModal = (props: Props) => {
  const { location, open, close } = props;
  const { t } = useTranslation();

  const { updateLocation, isLoading } = useLocationModal({ onUpdateSuccess: close });

  const form = useForm({
    defaultValues: {
      name: location.name,
    },
    onSubmit: async ({ value }) => {
      const validatedData = CreateLocationSchema.parse(value);
      const updatedLocation: Location = {
        ...location,
        name: validatedData.name,
      };
      await updateLocation(updatedLocation);
    },
  });

  if (location === null) return null;

  return (
    <BgtDialog open={open} onClose={close}>
      <BgtDialogContent>
        <form
          onSubmit={handleFormSubmit(form)}
          className="w-full"
        >
          <BgtDialogTitle>{t('location.edit.title')}</BgtDialogTitle>
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
