import { isApiError } from "@/models";

interface FormLike {
	handleSubmit: () => Promise<unknown>;
}

export const ignoreApiError = async (work: unknown) => {
	try {
		await work;
	} catch (error) {
		if (!isApiError(error)) {
			throw error;
		}
	}
};

export const handleFormSubmit = (form: FormLike) => (e: React.FormEvent) => {
	e.preventDefault();
	e.stopPropagation();
	return ignoreApiError(form.handleSubmit());
};
