import { useTranslation } from 'react-i18next';
import { memo } from 'react';
import { UseFormReturn } from '@tanstack/react-form';

import { CreateGame, CreateGameSchema } from '@/models';
import { BgtFormField, BgtInputField } from '@/components/BgtForm';

interface GameFormPlayerFieldsProps {
  form: UseFormReturn<CreateGame>;
  disabled: boolean;
}

const GameFormPlayerFieldsComponent = ({ form, disabled }: GameFormPlayerFieldsProps) => {
  const { t } = useTranslation();

  return (
    <div className="flex flex-row gap-2">
      <BgtFormField form={form} name="minPlayers" schema={CreateGameSchema.shape.minPlayers}>
        {(field) => (
          <BgtInputField
            field={field}
            label={t('game.new.manual.min-players.label')}
            type="number"
            disabled={disabled}
            className="pr-2"
          />
        )}
      </BgtFormField>
      <BgtFormField form={form} name="maxPlayers" schema={CreateGameSchema.shape.maxPlayers}>
        {(field) => (
          <BgtInputField
            field={field}
            label={t('game.new.manual.max-players.label')}
            type="number"
            disabled={disabled}
            className="pr-2"
          />
        )}
      </BgtFormField>
    </div>
  );
};

GameFormPlayerFieldsComponent.displayName = 'GameFormPlayerFields';

export const GameFormPlayerFields = memo(GameFormPlayerFieldsComponent);
