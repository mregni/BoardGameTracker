import { isApiError } from "@/models";

interface FormLike {
	handleSubmit: () => Promise<unknown>;
}

const FIELD_CONTROL = "input:not([type='hidden']), textarea, [role='combobox'], [role='spinbutton']";

const showFirstError = (form: Element) => {
	const error = form.querySelector("[data-slot='form-error']");
	error?.parentElement?.querySelector<HTMLElement>(FIELD_CONTROL)?.focus({ preventScroll: true });
	error?.scrollIntoView?.({ block: "center" });
};

export const ignoreApiError = async (work: unknown) => {
	try {
		await work;
	} catch (error) {
		if (!isApiError(error)) {
			throw error;
		}
	}
};

export const handleFormSubmit = (form: FormLike) => async (e: React.FormEvent) => {
	e.preventDefault();
	e.stopPropagation();
	const formElement = e.currentTarget;
	await ignoreApiError(form.handleSubmit());
	requestAnimationFrame(() => showFirstError(formElement));
};
