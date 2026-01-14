import { useTranslation } from 'react-i18next';
import { memo } from 'react';
import { UseFormReturn } from '@tanstack/react-form';

import { CreateGame, CreateGameSchema } from '@/models';
import { BgtFormField, BgtInputField } from '@/components/BgtForm';

interface GameFormTimeFieldsProps {
  form: UseFormReturn<CreateGame>;
  disabled: boolean;
}

const GameFormTimeFieldsComponent = ({ form, disabled }: GameFormTimeFieldsProps) => {
  const { t } = useTranslation();

  return (
    <>
      <div className="flex flex-row gap-2">
        <BgtFormField form={form} name="minPlayTime" schema={CreateGameSchema.shape.minPlayTime}>
          {(field) => (
            <BgtInputField
              field={field}
              label={t('game.new.manual.min-time.label')}
              type="number"
              disabled={disabled}
              className="pr-2"
              suffixLabel={t('common.minutes-abbreviation')}
            />
          )}
        </BgtFormField>
        <BgtFormField form={form} name="maxPlayTime" schema={CreateGameSchema.shape.maxPlayTime}>
          {(field) => (
            <BgtInputField
              field={field}
              label={t('game.new.manual.max-time.label')}
              type="number"
              disabled={disabled}
              className="pr-2"
              suffixLabel={t('common.minutes-abbreviation')}
            />
          )}
        </BgtFormField>
      </div>
      <BgtFormField form={form} name="minAge" schema={CreateGameSchema.shape.minAge}>
        {(field) => (
          <BgtInputField
            field={field}
            label={t('game.new.manual.min-age.label')}
            type="number"
            disabled={disabled}
            className="pr-2"
          />
        )}
      </BgtFormField>
    </>
  );
};

GameFormTimeFieldsComponent.displayName = 'GameFormTimeFields';

export const GameFormTimeFields = memo(GameFormTimeFieldsComponent);
