import { createFormHook, createFormHookContexts } from "@tanstack/react-form";
import { BgtDatePicker, type BgtDatePickerProps } from "@/components/BgtForm/BgtDatePicker";
import { BgtDateTimePicker, type BgtDateTimePickerProps } from "@/components/BgtForm/BgtDateTimePicker";
import { BgtInputField, type BgtInputFieldProps } from "@/components/BgtForm/BgtInputField";
import { BgtSelect, type BgtSelectProps } from "@/components/BgtForm/BgtSelect";
import { BgtSwitch, type BgtSwitchProps } from "@/components/BgtForm/BgtSwitch";
import { BgtTextArea, type BgtTextAreaProps } from "@/components/BgtForm/BgtTextArea";

export const { fieldContext, formContext, useFieldContext } = createFormHookContexts();

function AppInputField(props: Omit<BgtInputFieldProps, "field">) {
	const field = useFieldContext();
	return <BgtInputField field={field} {...props} />;
}

function AppSelect(props: Omit<BgtSelectProps, "field">) {
	const field = useFieldContext();
	return <BgtSelect field={field} {...props} />;
}

function AppDatePicker(props: Omit<BgtDatePickerProps, "field">) {
	const field = useFieldContext();
	return <BgtDatePicker field={field} {...props} />;
}

function AppDateTimePicker(props: Omit<BgtDateTimePickerProps, "field">) {
	const field = useFieldContext();
	return <BgtDateTimePicker field={field} {...props} />;
}

function AppTextArea(props: Omit<BgtTextAreaProps, "field">) {
	const field = useFieldContext();
	return <BgtTextArea field={field} {...props} />;
}

function AppSwitch(props: Omit<BgtSwitchProps, "field">) {
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
