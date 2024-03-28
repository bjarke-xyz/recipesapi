import http from 'k6/http';
import { check, sleep } from 'k6';

export default async function() {
	const query = `
	fragment SimpleRecipe on Recipe {
id
slug
  title
  description
  createdAt
  recipeReactions {
    favoritesCount
    userHasFavorited
    __typename
  }
  rating {
    score
    raters
    __typename
  }
  image {
    src
    thumbnails {
      large {
        src
        dimensions {
          width
          height
          __typename
        }
        __typename
      }
      __typename
    }
    dimensions {
      original {
        width
        height
        __typename
      }
      __typename
    }
    __typename
  }
  __typename
}

query GetRecipes($filter: RecipeFilterInput) {
  recipes(filter: $filter) {
    ...SimpleRecipe
    __typename
  }
}
	`
	const data = {
		operationName: "GetRecipes",
		variables: {
			filter: {
				orderByProperty: "createdAt",
				limit: 4
			}
		},
		query: query
	};
	const headers = {
		"Content-Type": "application/json",
	};
	let baseUrl = "https://recipesapi.bjarke.xyz"
	baseUrl = "http://localhost:5003"
	const res = http.post(`${baseUrl}/graphql`, JSON.stringify(data), { headers });
	check(res, { 'success get recipes': r => r.status === 200 });

	sleep(0.3);
}
