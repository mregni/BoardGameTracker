interface Props {
	errors?: string[];
	id?: string;
}

export const BgtFormErrors = ({ errors, id }: Props) => {
	if (!errors || errors.length === 0) {
		return null;
	}

	return (
		<div id={id} className="mt-1">
			{errors.map((error) => (
				<div key={error} className="text-error text-sm">
					{error}
				</div>
			))}
		</div>
	);
};
