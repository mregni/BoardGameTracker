import { createFormHook, createFormHookContexts } from "@tanstack/react-form";
import { BgtDatePicker, type BgtDatePickerBaseProps } from "@/components/BgtForm/BgtDatePicker";
import { BgtDateTimePicker, type BgtDateTimePickerProps } from "@/components/BgtForm/BgtDateTimePicker";
import { BgtInputField, type BgtInputFieldProps } from "@/components/BgtForm/BgtInputField";
import { BgtSelect, type BgtSelectBaseProps } from "@/components/BgtForm/BgtSelect";
import { BgtSwitch, type BgtSwitchProps } from "@/components/BgtForm/BgtSwitch";
import { BgtTextArea, type BgtTextAreaProps } from "@/components/BgtForm/BgtTextArea";

export const { fieldContext, formContext, useFieldContext } = createFormHookContexts();

function AppInputField(props: Readonly<Omit<BgtInputFieldProps, "field">>) {
	const field = useFieldContext();
	return <BgtInputField field={field} {...props} />;
}

function AppSelect(props: Readonly<BgtSelectBaseProps>) {
	const field = useFieldContext();
	return <BgtSelect field={field} {...props} />;
}

function AppDatePicker(props: Readonly<BgtDatePickerBaseProps>) {
	const field = useFieldContext();
	return <BgtDatePicker field={field} {...props} />;
}

function AppDateTimePicker(props: Readonly<Omit<BgtDateTimePickerProps, "field">>) {
	const field = useFieldContext();
	return <BgtDateTimePicker field={field} {...props} />;
}

function AppTextArea(props: Readonly<Omit<BgtTextAreaProps, "field">>) {
	const field = useFieldContext();
	return <BgtTextArea field={field} {...props} />;
}

function AppSwitch(props: Readonly<Omit<BgtSwitchProps, "field">>) {
	const field = useFieldContext();
	return <BgtSwitch field={field} {...props} />;
}

export const { useAppForm, withForm } = createFormHook({
	fieldContext,
	formContext,
	fieldComponents: {
		InputField: AppInputField,
		Select: AppSelect,
		DatePicker: AppDatePicker,
		DateTimePicker: AppDateTimePicker,
		TextArea: AppTextArea,
		Switch: AppSwitch,
	},
	formComponents: {},
});
